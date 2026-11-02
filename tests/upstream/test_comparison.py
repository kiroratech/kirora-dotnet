import importlib.util
import json
from pathlib import Path
import unittest

module = importlib.util.spec_from_file_location('comparison', Path(__file__).resolve().parents[2] / 'tools/compare-upstream.py')
c = importlib.util.module_from_spec(module)
module.loader.exec_module(c)

class ComparisonTests(unittest.TestCase):
    def spec(self):
        return {'openapi':'3.1.0','info':{'version':'1'},'paths':{'/x':{'get':{'operationId':'getX','responses':{'200':{'$ref':'#/components/schemas/X'}}}}},'components':{'schemas':{'X':{'type':'object','properties':{'description':{'type':'string'}}}}}}
    def compare(self, a, b, license=b'MIT'):
        return c.compare(a,b,b'MIT',license,'a'*40)
    def test_identical(self):
        a=json.dumps(self.spec()).encode()
        self.assertEqual('byte-identical',self.compare(a,a)['classification'])
    def test_formatting_equivalent(self):
        self.assertTrue(self.compare(json.dumps(self.spec()).encode(),json.dumps(self.spec(),indent=2).encode())['classification'].startswith('JSON-equivalent'))
    def test_real_property_named_description_preserved(self):
        a=self.spec(); b=self.spec(); b['components']['schemas']['X']['properties']['description']['type']='number'
        d=self.compare(json.dumps(a).encode(),json.dumps(b).encode())
        self.assertTrue(d['schemasChanged'][0]['structural'])
        self.assertEqual(['X'],d['endpointsChanged'][0]['referencedChangedSchemas'])
    def test_annotations_filtered(self):
        a=self.spec(); b=self.spec(); b['components']['schemas']['X']['description']='documentation'
        d=self.compare(json.dumps(a).encode(),json.dumps(b).encode())
        self.assertFalse(d['schemasChanged'][0]['structural'])
    def test_license_difference(self):
        a=json.dumps(self.spec()).encode()
        self.assertFalse(self.compare(a,a,b'changed')['licenseByteIdentical'])
    def test_removed_route(self):
        a=self.spec(); b=self.spec(); b['paths']={}
        d=self.compare(json.dumps(a).encode(),json.dumps(b).encode())
        self.assertEqual('getX',d['operationsRemoved'][0]['operationId'])
    def test_transitive_recursive_reference(self):
        a=self.spec(); a['components']['schemas']['X']['properties']['child']={'$ref':'#/components/schemas/X'}
        self.assertEqual({'X'},c.reachable(a['paths']['/x']['get'],a))

if __name__=='__main__': unittest.main()
