#!/usr/bin/env python3
# SDK native path/assembly reference checks. No game DLLs are redistributed.
# SDK 原生引用的静态回归测试，避免重复出现 Rider 无法解析 UnityEngine 的问题。
from pathlib import Path
import xml.etree.ElementTree as ET
import sys

ROOT = Path(__file__).resolve().parents[1]
NATIVE = ROOT / 'Native'

def check(ok, message):
    if not ok:
        raise AssertionError(message)

paths = NATIVE / 'build' / 'NativeInteropPaths.props'
targets = NATIVE / 'build' / 'NativeInteropReferences.targets'
csproj = NATIVE / 'AstralParty.Modding.SDK.Native.csproj'
for path in (paths, targets, csproj, ROOT / 'AstralParty.Modding.SDK.csproj'):
    check(path.exists(), f'Missing {path}')
    ET.parse(path)
    print(f'[OK] XML {path.relative_to(ROOT)}')
root = ET.parse(targets).getroot()
check(not any(node.tag == 'Error' for node in root), 'MSBuild Error task must be nested inside a Target')
check(any(node.find('Error') is not None for node in root.findall('Target')), 'Missing validation Target')
proxy_items = root.findall('.//_AstralNativeProxy')
check(any('UnityEngine*.dll' in item.get('Include', '') for item in proxy_items), 'UnityEngine module wildcard missing')
check(any('Il2Cppmscorlib.dll' in item.get('Include', '') for item in proxy_items), 'Il2Cppmscorlib proxy missing')
check(any('Il2CppSystem*.dll' in item.get('Include', '') for item in proxy_items), 'Il2CppSystem proxies missing')
refs = root.findall('.//Reference')
check(any('@(_AstralNativeProxy)' in r.get('Include', '') and r.findtext('Private') == 'false' for r in refs), 'Proxy references must be local=False')
check("'$(DesignTimeBuild)' != 'true'" in targets.read_text(), 'Do not fail Rider design time without game DLLs')
imports = ET.parse(csproj).getroot().findall('Import')
include = [node.get('Project') for node in imports]
check(include.index('build/NativeInteropPaths.props') < include.index('build/NativeInteropReferences.targets'), 'Path import must precede targets')
check('NativeLocal.props' in include, 'SDK standalone config not available')
check('AstralInteropDir' in paths.read_text(), 'Missing early proxy resolution')
check('0.10.1' in csproj.read_text(), 'SDK Native version not updated')
print('[PASS] SDK native reference regression checks')
