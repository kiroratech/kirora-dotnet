"""Small deterministic XML documentation helper for public declarations."""
import re

def document(text):
 result=[]
 for line in text.splitlines():
  if re.match(r'\s*public ',line):
   index=len(result)
   while index and result[index-1].lstrip().startswith('['):index-=1
   if not index or not result[index-1].lstrip().startswith('///'):
    name=re.search(r'(?:class|struct|record)\s+(\w+)',line)
    description=(name.group(1)+' Kirora API contract.' if name else 'Provides the documented '+('operation.' if '(' in line else 'configuration or response value.'))
    result.insert(index,' '*(len(line)-len(line.lstrip()))+'/// <summary>'+description+'</summary>')
  result.append(line)
 return '\n'.join(result)+'\n'
