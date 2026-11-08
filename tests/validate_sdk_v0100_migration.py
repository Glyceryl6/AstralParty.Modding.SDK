#!/usr/bin/env python3
"""SDK v0.10.0 native split static checks / SDK Native 迁移静态检查。"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
NATIVE = ROOT / "Native"
GROUPS = {"Interop": 22, "UI": 12}


def verify() -> None:
    assert not (ROOT / "AstralParty.Modding.SDK").exists(), "redundant nested SDK folder remains"
    core = ET.parse(ROOT / "AstralParty.Modding.SDK.csproj").getroot()
    assert not (NATIVE / "AstralParty.Modding.SDK.Native.csproj").exists()
    assert not any("Native/**/*.cs" in x.attrib.get("Remove", "") for x in core.iter("Compile"))
    version = ET.parse(ROOT / "AstralParty.Modding.SDK.csproj").findtext(".//Version")
    assert version is not None and tuple(map(int, version.split("."))) >= (0, 11, 0), version
    assert (ROOT / "docs" / "README-v0.8.0.md").is_file()
    assert (ROOT / "tests/CoreServices/CoreServices.csproj").is_file()
    assert (ROOT / "Interop/Il2CppNative.cs").is_file()
    for name, count in GROUPS.items():
        paths = sorted((NATIVE / name).glob("*.cs"))
        assert len(paths) == count, (name, len(paths))
        for path in paths:
            text = path.read_text(encoding="utf-8-sig")
            assert f"namespace AstralParty.Modding.SDK.Native.{name}" in text, path
            assert "AstralProbe.Framework" not in text, path
            assert not re.search(r"\b(?:Plugin|ModSettingsLayout|ModLocalization)\.", text), path
            assert re.search(r"\bpublic\s+(?:static\s+|sealed\s+|partial\s+|unsafe\s+)*class\s+", text), path
    services = (NATIVE / "NativeSdkServices.cs").read_text()
    assert "public static void Configure" in services
    assert "ActionButtonFontSize" in services and "EnumLabelAssemblyPath" in services
    assert (ROOT / "build/NativeInteropReferences.targets").is_file()
    print("SDK v0.10.0: 34 native files, native sources and callbacks verified / 检查通过")


if __name__ == "__main__":
    verify()
