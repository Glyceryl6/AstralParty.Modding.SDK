#!/usr/bin/env python3
"""SDK v0.11.0: single assembly project checks / 单 DLL SDK 配置检查。"""
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
project = root / 'AstralParty.Modding.SDK.csproj'
tree = ET.parse(project).getroot()
assert tree.findtext('.//AssemblyName') == 'AstralParty.Modding.SDK'
assert tree.findtext('.//Version') == '0.12.0'
assert not (root / 'Native/AstralParty.Modding.SDK.Native.csproj').exists()
assert not any('Native/' in item.attrib.get('Remove', '') for item in tree.iter('Compile'))
assert any('SdkLocal.props' in item.attrib.get('Project', '') for item in tree.iter('Import'))
assert any('NativeInteropPaths.props' in item.attrib.get('Project', '') for item in tree.iter('Import'))
assert any('NativeInteropReferences.targets' in item.attrib.get('Project', '') for item in tree.iter('Import'))
packages = {node.attrib['Include'] for node in tree.iter('PackageReference')}
for package in ('SNWCreations.Il2CppInterop.Runtime', 'SNWCreations.Il2CppInterop.Common',
                'IL2CppSharp', 'IL2CppSharp.Hooking'):
    assert package in packages, package
assert not list(root.rglob('AstralParty.Modding.SDK.Native.csproj'))
for category, count in [('Runtime', 12), ('Assets', 7), ('Controls', 10), ('Widgets', 10), ('Gameplay', 1)]:
    sources = list((root / 'Native' / category).glob('*.cs'))
    assert len(sources) == count, (category, len(sources))
    assert all('namespace AstralParty.Modding.SDK.Native' in file.read_text(encoding='utf-8-sig')
               for file in sources)
for name in ('NativeInteropPaths.props', 'NativeInteropReferences.targets'):
    ET.parse(root / 'build' / name)
references = ET.parse(root / 'build/NativeInteropReferences.targets').getroot()
assert any('UnityEngine*.dll' in e.get('Include', '') for e in references.iter('_AstralNativeProxy'))
assert not any(c.tag == 'Error' for c in references)
assert (root / 'SdkLocal.props.example').exists()
print('[PASS] SDK current unified project, 40 native sources and references')
