#!/usr/bin/env python3
"""Verify SDK v0.12.0 Native consolidation and GitHub documents."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
assert ET.parse(root / 'AstralParty.Modding.SDK.csproj').findtext('.//Version') == '0.12.0'
assert (root / 'README.md').is_file()
assert (root / 'wiki/Home.md').is_file() and (root / 'wiki/_Sidebar.md').is_file()
assert len(list((root / 'wiki').glob('*.md'))) >= 7
for old in ['Interop', 'UI', 'Native/Interop', 'Native/UI']:
    assert not list((root / old).glob('*.cs')), old
for stale in ['Game/HeroPreviewOrdering.cs', 'Media/PngFiles.cs',
              'Replay/ReplayArchivePolicy.cs', 'Statistics/AchievementPresentation.cs']:
    assert not (root / stale).exists(), stale
parts = ['Runtime', 'Assets', 'Controls', 'Widgets', 'Gameplay']
files = [f for part in parts for f in (root / 'Native' / part).glob('*.cs')]
assert len(files) == 40, len(files)
for f in files:
    code = f.read_text(encoding='utf-8-sig')
    assert re.search(r'namespace\s+AstralParty\.Modding\.SDK\.Native[;\s{]', code), f
    assert 'AstralParty.Modding.SDK.Native.Interop' not in code, f
    assert 'AstralParty.Modding.SDK.Native.UI' not in code, f
assert 'using AstralParty.Modding.SDK.Native;' in (root / 'Runtime/SdkHost.cs').read_text()
for rel in ['Native/Widgets/FairyBridge.cs', 'Native/Runtime/Il2CppNative.cs',
            'Native/Controls/NativeActionButton.cs', 'Native/NativeSdkServices.cs']:
    assert (root / rel).read_text(encoding='utf-8-sig').count('/// <summary>') >= 4, rel
ET.parse(root / 'build/NativeInteropReferences.targets')
print('[PASS] SDK v0.12.0 Native layout, namespace, comments, docs and XML')
