#!/usr/bin/env python3
"""Generate core SDK models, query contracts, operation metadata and resource methods.
Only reachable schemas are generated. The pinned spec is never modified.
"""
import json,hashlib,re,sys
from xml.sax.saxutils import escape
from codegen_xml import document
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=json.loads((root/'spec/openapi.json').read_text());source=json.loads((root/'spec/source.json').read_text())
if hashlib.sha256((root/'spec/openapi.json').read_bytes()).hexdigest()!=source['sha256']:raise SystemExit('Pinned specification checksum mismatch')
if spec['info']['version']!=source['version']:raise SystemExit('Pinned API version mismatch')
base={'ApiCaller','FieldError'}
classes={};converters=[];known=set();outputs={};operations=[];union_shapes={};serial_types=set();optional_types=set()
methodnames={
'listContacts':'List','identifyContact':'Identify','identifyContactsBatch':'IdentifyBatch','getContact':'Get','updateContact':'Update','deleteContact':'Delete','deleteContacts':'DeleteBatch','mergeContacts':'Merge','listContactConsentChanges':'ListConsentChanges','getContactExport':'Export','listContactLists':'ListLists','listContactSegments':'ListSegments','listContactTopics':'ListTopics','setContactTopic':'SetTopic','listContactEvents':'ListEvents','listContactJourneys':'ListJourneys','listContactMessages':'ListMessages','listPushInstallations':'ListInstallations','registerPushInstallation':'RegisterInstallation','deletePushInstallation':'DeleteInstallation',
'listLists':'List','createList':'Create','getList':'Get','updateList':'Update','deleteList':'Delete','addListContacts':'AddContacts','deleteListContacts':'RemoveContacts','getListUsage':'GetUsage','trackEvent':'Track','trackEventsBatch':'TrackBatch','sendTransactionalMessage':'Send','sendTransactionalMessagesBatch':'SendBatch','cancelScheduledTransactionalMessage':'Cancel','getMessage':'Get','getMessageContent':'GetContent'}
def pascal(s):return ''.join(x[:1].upper()+x[1:] for x in re.split('[^A-Za-z0-9]+',s))
def resolve(s):return spec['components']['schemas'][s['$ref'].split('/')[-1]] if '$ref' in s else s
def nullable(s):
 if '$ref' in s:return nullable(resolve(s))
 if isinstance(s.get('type'),list):return 'null' in s['type']
 return any(x.get('type')=='null' for x in s.get('anyOf',[])) or not any(k in s for k in ['type','$ref','anyOf','oneOf'])
def ctype(s,name):
 t=_ctype(s,name)
 if t.endswith('?') and t[:-1] not in ['Guid','DateTimeOffset','DateOnly','bool','long','decimal','double','JsonElement']:serial_types.add(t[:-1])
 else:serial_types.add(t)
 return t

def _ctype(s,name):
 unsupported=set(s)&{'allOf','not','if','then','else','patternProperties','unevaluatedProperties','prefixItems'}
 if unsupported:raise ValueError(f'Unsupported schema constructs in {name}: {sorted(unsupported)}')
 if '$ref' in s:
  n=s['$ref'].split('/')[-1]
  n={'Attribute':'ContactAttribute'}.get(n,n)
  if n not in base:return ctype(resolve(s),n)
  return n
 kinds=s.get('type')
 if isinstance(kinds,list):
  nonnull=[k for k in kinds if k!='null']
  if len(nonnull)>1:return ctype({'anyOf':[dict(s,type=k) for k in nonnull]},name)+('?' if 'null' in kinds else '')
  return ctype(dict(s,type=nonnull[0]),name)+'?'
 union=s.get('oneOf',s.get('anyOf'))
 if union and 'properties' not in s and kinds is None:
  nonnull=[a for a in union if a.get('type')!='null']
  if len(nonnull)==1:return ctype(nonnull[0],name)+('?' if len(nonnull)!=len(union) else '')
  # A UUID-or-alias is a single string contract, not a type-discriminated union.
  if all(resolve(a).get('type')=='string' for a in nonnull):return 'string'
  if name not in known:
   known.add(name);classes[name]=''
   variants=[];shapes=[]
   for i,a in enumerate(nonnull):
    vname=name+pascal(a.get('title',f'Variant{i+1}'))
    v=ctype(a,vname)
    if v.startswith('IReadOnlyList<'):v=v[len('IReadOnlyList<'):-1]+'[]';serial_types.add(v)
    variants.append(v);shapes.append(a)
   variants=list(dict.fromkeys(variants))
   union_shapes[name]=nonnull
   members=['[JsonConverter(typeof('+name+'Converter))]',f'public sealed class {name}', '{','    public object Value { get; }','    private '+name+'(object value) => Value = value;', '    public T As<T>() => Value is T value ? value : throw new InvalidOperationException("The union holds another variant.");']
   for v in variants:members.append(f'    public static implicit operator {name}({v} value) => new(value ?? throw new ArgumentNullException(nameof(value)));' if v not in ['bool','long','decimal','double','Guid','DateTimeOffset','DateOnly','JsonElement'] else f'    public static implicit operator {name}({v} value) => new(value);')
   members+=['}'];classes[name]='\n'.join(members)
   code=[f'internal sealed class {name}Converter : JsonConverter<{name}>','{',f'    public override {name}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)','    {','        using var document = JsonDocument.ParseValue(ref reader);','        var element = document.RootElement;']
   for i,v in enumerate(variants):code += [f'        if (!Kirora.Serialization.ContractValidator.MatchesUnionVariant({json.dumps(name)}, {i}, element)) goto next{i};', '        try', '        {',f'            return ({name})JsonSerializer.Deserialize(element, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<{v}>)options.GetTypeInfo(typeof({v})))!;', '        }','        catch (JsonException) { }',f'        next{i}:;']
   code += ['        throw new JsonException("No documented union variant matched.");','    }',f'    public override void Write(Utf8JsonWriter writer, {name} value, JsonSerializerOptions options)','    {','        switch (value.Value)','        {']
   for v in variants:code.append(f'            case {v} typed: JsonSerializer.Serialize(writer, typed, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<{v}>)options.GetTypeInfo(typeof({v}))); break;')
   code+=['            default: throw new JsonException("Unsupported union value.");','        }','    }','}'];converters.append('\n'.join(code))
  return name
 if kinds=='array':return 'IReadOnlyList<'+ctype(s['items'],name+'Item')+'>'
 if kinds=='object' or 'properties' in s:
  if not s.get('properties'):
   ap=s.get('additionalProperties',{})
   if ap is False:
    ap={}
   return 'Dictionary<string, '+(ctype(ap,name+'Value') if isinstance(ap,dict) else 'JsonElement')+'>'
  if name not in known:
   known.add(name);classes[name]=''
   members=[f'public sealed partial class {name}','{']
   # Union variants are closed objects so required-field alternatives cannot swallow each other.
   for field,shape in s['properties'].items():
    t=ctype(shape,name+pascal(field));required=field in s.get('required',[])
    prop=pascal(field)
    if prop==name:prop+='Value'
    description=' '.join(shape.get('description',f'The documented {field} value.').split())
    if field not in s.get('required',[]):description += ' Omit this field to leave it unspecified.'
    members += ['    /// <summary>'+escape(description[:1400])+'</summary>', f'    [JsonPropertyName({json.dumps(field)})]']
    if not required:
     members += ['    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]']
     optional_types.add(t)
     t='Optional<'+t+'>'
    members += [f'    public {"required " if required else ""}{t} {prop} {{ get; init; }}','']
   if s.get('additionalProperties',False) is not False:
    members += ['    [JsonExtensionData]','    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }']
   members+=['}'];classes[name]='\n'.join(members)
  return name
 if kinds=='string' and s.get('format')=='binary':return 'byte[]'
 if kinds=='string':
  # String-backed enum values remain forward compatible. UUID-or-alias unions retain string.
  return {'uuid':'Guid','date-time':'DateTimeOffset','date':'DateOnly'}.get(s.get('format'),'string')
 if kinds=='integer':return 'long'
 if kinds=='number':return 'decimal'
 if kinds=='boolean':return 'bool'
 if kinds is None:return 'JsonElement'
 raise ValueError((name,s))

def add_docs(text):
 return document(text)

def write(path,text):outputs[path]=add_docs(text) if path.endswith('.cs') else text

resource_names = {tag['name']: pascal(tag['name']) for tag in spec['tags']}
resource_names.update({'me':'Me','shared-blocks':'SharedBlocks','sending-domains':'SendingDomains','push-projects':'PushProjects','conversion-goals':'ConversionGoals','embed-sessions':'EmbedSessions'})
for item in spec['paths'].values():
 for op in item.values():
  if not isinstance(op,dict) or 'operationId' not in op:continue
  oid=op['operationId'];tag=op['tags'][0]
  if oid in methodnames:continue
  noun=resource_names[tag]
  singular={'Journeys':'Journey','Messages':'Message','Templates':'Template','Campaigns':'Campaign','Segments':'Segment','Topics':'Topic','Attributes':'Attribute','Tags':'Tag','ConversionGoals':'ConversionGoal','SharedBlocks':'SharedBlock','Assets':'Asset','Forms':'Form','Suppressions':'Suppression','SendingDomains':'SendingDomain','Senders':'Sender','PushProjects':'PushProject','Webhooks':'Webhook','Workspaces':'Workspace','EmbedSessions':'EmbedSession','Events':'Event'}.get(noun,noun)
  candidate=pascal(oid)
  for token in sorted({noun,singular},key=len,reverse=True):candidate=candidate.replace(token,'')
  methodnames[oid]=candidate
methodnames.update({'getMe':'Get','getEventSummary':'GetSummary','getEventSeries':'GetSeries','listEventCatalog':'ListCatalog','listStandardEventNames':'ListStandardNames','defineEventName':'DefineName','deleteEventName':'DeleteName','listCampaignReports':'ListCampaigns','getOverviewReport':'GetOverview','getTransactionalReport':'GetTransactional','getFunnelReport':'GetFunnel','getRfmSummary':'GetRfmSummary','downloadMessageExport':'DownloadExportTo'})

resource_methods={r:[] for r in resource_names.values()}
for path,item in spec['paths'].items():
 for method,o in item.items():
  if not isinstance(o,dict) or o.get('operationId') not in methodnames:continue
  oid=o['operationId'];name=pascal(oid);tag=o['tags'][0]
  resource=resource_names[tag]
  params=[]
  for p in o.get('parameters',[]):
   if '$ref' in p:p=spec['components']['parameters'][p['$ref'].split('/')[-1]]
   params.append(p)
  paths=[p for p in params if p['in']=='path'];queries=[p for p in params if p['in']=='query']
  querytype=ctype({'type':'object','properties':{p['name']:p['schema'] for p in queries},'additionalProperties':False,'required':[p['name'] for p in queries if p.get('required')]},name+'Query') if queries else None
  multipart='multipart/form-data' in o.get('requestBody',{}).get('content',{})
  body=o.get('requestBody',{}).get('content',{}).get('multipart/form-data' if multipart else 'application/json',{}).get('schema')
  bodytype=ctype(body,name+'Input') if body else None
  success=next((v for k,v in o['responses'].items() if k.startswith('2')),None)
  csv='text/csv' in success.get('content',{})
  schema=success.get('content',{}).get('application/json',{}).get('schema')
  responsetype=ctype(schema,name+'Response') if schema else None
  haskey=any(p['name'].lower()=='idempotency-key' for p in params)
  retry=o.get('x-retry','read' if method=='get' else 'unsafe')
  entries='events' if oid=='trackEventsBatch' else 'messages' if oid=='sendTransactionalMessagesBatch' else None
  info=dict(id=oid,name=name,resource=resource,method=method.upper(),path=path,querytype=querytype,bodytype=bodytype,responsetype=responsetype,retry=retry,key=haskey,entries=entries,paths=[p['name'] for p in paths],summary=o['summary'],methodName=methodnames[oid],multipart=multipart,csv=csv)
  operations.append(info)
  args=[f'string {p["name"]}' for p in paths]
  if bodytype:args.append(f'{bodytype} body' if o['requestBody'].get('required') else f'{bodytype}? body = null')
  if multipart:args += ['string fileName','string contentType']
  if csv:args.append('Stream destination')
  if querytype:args.append(f'{querytype} query' if any(p.get('required') for p in queries) else f'{querytype}? query = null')
  args+=['KiroraRequestOptions? options = null','CancellationToken cancellationToken = default']
  apath=json.dumps(path)
  for p in paths:apath=apath.replace('{'+p['name']+'}', '{ResourceHelpers.Segment('+p['name']+')}')
  if paths:apath='$'+apath
  calls=f'Operations.{name}, {apath}, '+('body, fileName, contentType' if multipart else 'destination' if csv else ('body' if bodytype else 'null')+', '+('query' if querytype else 'null'))+', options, cancellationToken'
  rt='Task<'+responsetype+'>' if responsetype else 'Task'
  fn='SendMultipartAsync<'+responsetype+'>' if multipart else 'DownloadToAsync' if csv else 'SendAsync<'+responsetype+'>' if responsetype else 'SendNoContentAsync'
  lines=[f'    /// <summary>{o["summary"].replace("&","&amp;")}. Operation: {oid}.</summary>',f'    /// <remarks>Scope: {o.get("x-required-scope","any valid key")}. Retry policy: {retry}. See the operation contract for key-kind restrictions.</remarks>',f'    public {rt} {methodnames[oid]}Async({", ".join(args)}) =>',f'        transport.{fn}({calls});','']
  resource_methods[resource]+=lines
  if schema and 'meta' in schema.get('properties',{}):
   meta=resolve(schema['properties']['meta'])
   if 'pagination' in meta.get('properties',{}):
    data=resolve(schema['properties']['data']);it=ctype(data['items'],name+'ResponseDataItem')
    allargs=[f'string {p["name"]}' for p in paths]+([f'{querytype} query' if any(p.get('required') for p in queries) else f'{querytype}? query = null'] if querytype else [])+['KiroraRequestOptions? options = null','CancellationToken cancellationToken = default']
    resource_methods[resource]+=[f'    /// <summary>Lazily enumerates all pages for {oid}, preserving filters and cancellation.</summary>',f'    public IAsyncEnumerable<{it}> {methodnames[oid]}AllAsync({", ".join(allargs)}) =>',f'        transport.EnumerateAsync<{responsetype}, {it}>(Operations.{name}, {apath}, query, options,', '            page => page.Data, page => page.Meta.Pagination.NextCursor, cancellationToken);','']

header='// <auto-generated>Generated from the pinned Kirora OpenAPI specification by tools/generate-core.py.</auto-generated>\n#nullable enable\n'
write('src/Kirora/Models/CoreModels.g.cs',header+'using System.Text.Json;\nusing System.Text.Json.Serialization;\nusing Kirora.Serialization;\n\nnamespace Kirora.Models;\n\n'+'\n\n'.join(classes.values())+'\n\n'+'\n\n'.join(converters)+'\n')
for r,methods in resource_methods.items():
 write(f'src/Kirora/Resources/{r}Resource.g.cs',header+'using Kirora.Http;\nusing Kirora.Models;\n\nnamespace Kirora.Resources;\n\n'+f'public sealed class {r}Resource\n{{\n    private readonly KiroraHttpTransport transport;\n    internal {r}Resource(KiroraHttpTransport transport) => this.transport = transport;\n\n'+'\n'.join(methods)+'}\n')
write('src/Kirora/Http/Operations.g.cs',header+'namespace Kirora.Http;\n\ninternal static class Operations\n{\n'+'\n'.join(f'    internal static readonly Operation {o["name"]} = new({json.dumps(o["id"])}, {json.dumps(o["method"])}, {json.dumps(o["retry"])}, {str(o["key"]).lower()}, {json.dumps(o["entries"]) if o["entries"] else "null"});' for o in operations)+'\n}\n')
# Source generation discovers nested model members; union variant types also need explicit roots.
write('src/Kirora/Http/CoreJsonContext.g.cs',header+'using System.Text.Json;\nusing System.Text.Json.Serialization;\nusing Kirora.Models;\n\nnamespace Kirora.Http;\n\n[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]\n'+ '\n'.join(f'[JsonSerializable(typeof({n}), TypeInfoPropertyName = "Core{n}")]' for n in classes)+ '\n'+'\n'.join(f'[JsonSerializable(typeof({t}))]' for t in sorted(serial_types-set(classes)))+'\ninternal partial class KiroraCoreJsonContext : JsonSerializerContext;\n')
# Statically root every SDK Optional converter; no MakeGenericType is needed for SDK models in Native AOT.
optional_code = header + 'using System.Text.Json.Serialization;\n\nnamespace Kirora.Serialization;\n\ninternal static class GeneratedOptionalConverters\n{\n    internal static JsonConverter? Create(Type type)\n    {\n'
for t in sorted(optional_types):
 optional_code += f'        if (type == typeof(Optional<{t}>)) return new OptionalJsonConverter<{t}>();\n'
optional_code += '        return null;\n    }\n}\n'
optional_code = optional_code.replace('using System.Text.Json.Serialization;', 'using System.Text.Json;\nusing System.Text.Json.Serialization;\nusing Kirora.Models;')
write('src/Kirora/Serialization/OptionalConverters.g.cs', optional_code)

write('spec/core-operations.json',json.dumps(operations,indent=2)+'\n')
write('spec/core-unions.json',json.dumps(union_shapes,indent=2)+'\n')
for path,text in outputs.items():
 p=root/path
 if '--check' in sys.argv:
  if not p.exists() or p.read_text()!=text:raise SystemExit('Generated file out of date: '+path)
 else:p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text)
print(f'Core generation verified: {len(operations)} operations, {len(classes)} typed models/unions.')
