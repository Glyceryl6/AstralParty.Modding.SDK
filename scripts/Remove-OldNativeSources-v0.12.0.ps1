<#
.SYNOPSIS
  Preview or safely move SDK v0.11.0 legacy Native source files out of the project.
.DESCRIPTION
  默认只预览。加 -Apply 才移动；只处理与 v0.11.0 基线 SHA256 一致的旧文件。
  发现有本地修改的旧文件时绝不强删，需开发者手动比对新位置并处理。
.EXAMPLE
  .\scripts\Remove-OldNativeSources-v0.12.0.ps1
  .\scripts\Remove-OldNativeSources-v0.12.0.ps1 -Apply
#>
param(
    [string]$SdkRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $SdkRoot).Path
$legacy = @(
    @{ Old = 'Interop/Il2CppNative.cs'; New = 'Native/Runtime/Il2CppNative.cs'; Sha256 = '26C385BEEFAC2F4A87182406389BAFB8E7CB44D2E499F751BA787CED98500579' }
    @{ Old = 'Interop/NativeTypedFields.cs'; New = 'Native/Runtime/NativeTypedFields.cs'; Sha256 = '62572B733A6A972950DE68DA2A6B565685348AB7315F08CADF3BF3918E576547' }
    @{ Old = 'UI/NativeUiCatalog.cs'; New = 'Native/Widgets/NativeUiCatalog.cs'; Sha256 = '414213B6B9B95985966186C2266F2AB9AC918DF4069E72ABD78ED52512AFC3E4' }
    @{ Old = 'UI/NativeUiEnums.cs'; New = 'Native/Widgets/NativeUiEnums.cs'; Sha256 = '59563376907425D236A2A6D1377045B5A40F0D4070333B7EEE3FD9A1E18303FA' }
    @{ Old = 'UI/NativeUiLayout.cs'; New = 'Native/Widgets/NativeUiLayout.cs'; Sha256 = 'E9852E012F3E113EA28CA23F4F7D75D352454C17692C717400E3DD08AA3D165D' }
    @{ Old = 'UI/UiContracts.cs'; New = 'Native/Widgets/UiContracts.cs'; Sha256 = '068907B605885A21CEC856F9F4844B1986021D80CD4B6F7ABC8E821965630391' }
    @{ Old = 'Native/Interop/CheckedNativeCall.cs'; New = 'Native/Runtime/CheckedNativeCall.cs'; Sha256 = '324D456035D16A532863E98834E4CA2133E4C8888892788C425990BC66EA7A68' }
    @{ Old = 'Native/Interop/GameConditionTextReader.cs'; New = 'Native/Assets/GameConditionTextReader.cs'; Sha256 = '6E3B322D7E34361D650250D73DA2B9F10642B706301B600A61E4CC7707379C35' }
    @{ Old = 'Native/Interop/GameRuntimeCapabilities.cs'; New = 'Native/Runtime/GameRuntimeCapabilities.cs'; Sha256 = '9D91242674A4265132D2EE0954D3673EFCD885AF3241C5FE1651FA780314D67F' }
    @{ Old = 'Native/Interop/HotfixEnumPresentation.cs'; New = 'Native/Assets/HotfixEnumPresentation.cs'; Sha256 = 'ED4AB2AC012C3EBE37A9F5BE154152A815CB5A8618B4AD5162B8FA34827F8B24' }
    @{ Old = 'Native/Interop/InspectorEnumLabels.cs'; New = 'Native/Assets/InspectorEnumLabels.cs'; Sha256 = '316F9EC3568B1A593039BB9006750966BFC517EEC86AF9ABBAAA48652670D48B' }
    @{ Old = 'Native/Interop/NativeActionButton.cs'; New = 'Native/Controls/NativeActionButton.cs'; Sha256 = '927195C00BD7BDCF9B8D871C2A0B59CA3772124077B719BE2C0E778202329642' }
    @{ Old = 'Native/Interop/NativeAssetLookup.cs'; New = 'Native/Assets/NativeAssetLookup.cs'; Sha256 = 'D76E51FD7A25F36754CA0CD330F29F7E33FB7F86E3B6AB220E5CE92B8A612F09' }
    @{ Old = 'Native/Interop/NativeBoxedTask.cs'; New = 'Native/Runtime/NativeBoxedTask.cs'; Sha256 = 'CE94F8A61646E631BBB9B416282202B5D0FD9317478933A59890BCFB466C2A9C' }
    @{ Old = 'Native/Interop/NativeButtonPolling.cs'; New = 'Native/Controls/NativeButtonPolling.cs'; Sha256 = '6CB2FE260CC3EB79AFB0037919819BB0F7EBAF6B74C20DD6EF1E0A61C5EE3509' }
    @{ Old = 'Native/Interop/NativeCatalogLookup.cs'; New = 'Native/Assets/NativeCatalogLookup.cs'; Sha256 = '2757C62B56ACD2F8BADC1E96E10E45646187700191FF48B46C1B224CBB77E77D' }
    @{ Old = 'Native/Interop/NativeDelegateFactory.cs'; New = 'Native/Runtime/NativeDelegateFactory.cs'; Sha256 = '39F24264E816843008201886191914A5260B80E60927B2269893B47F7D9F1E5F' }
    @{ Old = 'Native/Interop/NativeEventSuspension.cs'; New = 'Native/Runtime/NativeEventSuspension.cs'; Sha256 = '241424B6B14B00F55135CC66A8AAD514087022C225E2D5359652D62EC93EA289' }
    @{ Old = 'Native/Interop/NativeGame.cs'; New = 'Native/Runtime/NativeGame.cs'; Sha256 = '5094C8318EA19BDF295C0FECFC9BF4C7CA2E80E1FF8D48E816A945E7BBC243A0' }
    @{ Old = 'Native/Interop/NativeHeroCosmeticEligibility.cs'; New = 'Native/Gameplay/NativeHeroCosmeticEligibility.cs'; Sha256 = '4AA0BB2A92DC6AC4CF2E74E43EE53AA83F8BEF6233B53EBDCE1BEA3E62A3BC4E' }
    @{ Old = 'Native/Interop/NativeObjectLease.cs'; New = 'Native/Runtime/NativeObjectLease.cs'; Sha256 = '6950DAC76D790E4BF41F235601DC946C258ECDC3F57E3E677CA2D54930DAF123' }
    @{ Old = 'Native/Interop/NativePackageBinding.cs'; New = 'Native/Runtime/NativePackageBinding.cs'; Sha256 = '46C32E81BC488A29761650616E9714A3811317A4FBB014A39983372C0373C78A' }
    @{ Old = 'Native/Interop/NativePreviewList.cs'; New = 'Native/Assets/NativePreviewList.cs'; Sha256 = 'F11B234212F99B0D666F876605B29904F45000F3C68945040C7178A33B37E15D' }
    @{ Old = 'Native/Interop/NativeReferenceFields.cs'; New = 'Native/Runtime/NativeReferenceFields.cs'; Sha256 = 'EF787A171181CA54034F9B0A06EC66FD4630820A462800540F4F66C8455687D5' }
    @{ Old = 'Native/Interop/NativeToggleControl.cs'; New = 'Native/Controls/NativeToggleControl.cs'; Sha256 = '6BB6E8EB31CE3CB85B3024B00482D43B01B63FA0AFC2EDD68A84E2A027BCE975' }
    @{ Old = 'Native/Interop/NativeToggleLayout.cs'; New = 'Native/Controls/NativeToggleLayout.cs'; Sha256 = 'DB18F50EE4B280FCB9F1EC1B0E92100460FC892780BFB552B4B7F0ABE46B7DB8' }
    @{ Old = 'Native/Interop/NativeValue.cs'; New = 'Native/Runtime/NativeValue.cs'; Sha256 = 'BCDC06D58312A2185DF25FA5461067ED0AF6E8510BCF575689EAAE90768A6850' }
    @{ Old = 'Native/Interop/RuntimeEnumReflection.cs'; New = 'Native/Assets/RuntimeEnumReflection.cs'; Sha256 = '3BDAB3E460B4608F66B207E05748899DE85190EC908E9C034F4D3E36DF9D17B0' }
    @{ Old = 'Native/UI/FairyBridge.cs'; New = 'Native/Widgets/FairyBridge.cs'; Sha256 = '4A644CB28B4DF9299CFFA4D5E0CF642BE3018970BC34515C26E76E08ABC8A960' }
    @{ Old = 'Native/UI/GameToastService.cs'; New = 'Native/Widgets/GameToastService.cs'; Sha256 = '24081A73C65D8BF715CA599A794BBDED12FBC4371D99E48894661F0AA7FAF350' }
    @{ Old = 'Native/UI/GameUiState.cs'; New = 'Native/Widgets/GameUiState.cs'; Sha256 = 'A8863D372D4BBC8AA4FBCAE873D1019DF3C92C09023D4988C06C36416BB88900' }
    @{ Old = 'Native/UI/ModCaptionOutline.cs'; New = 'Native/Widgets/ModCaptionOutline.cs'; Sha256 = '7B2A6C40EA73DF010E3A65F537F1022FD118ECE2EE4370E358A2AF083041D941' }
    @{ Old = 'Native/UI/NativeFairyText.cs'; New = 'Native/Controls/NativeFairyText.cs'; Sha256 = '4D7A870E2BDA646BC5A47BE82FB8AAD4213342216F7050FC306F6C1D8D556A86' }
    @{ Old = 'Native/UI/NativeMailActionButton.cs'; New = 'Native/Controls/NativeMailActionButton.cs'; Sha256 = '2693B9E26BAA8D5E90FF7655B48C3A9B0E46F8D4D130862A00A6964B04984A21' }
    @{ Old = 'Native/UI/NativeModeListScroller.cs'; New = 'Native/Controls/NativeModeListScroller.cs'; Sha256 = '4F16C9D531F8CF37E3B4158B78ED26C80FF9B31FF670B89A3253F42CF8FBA524' }
    @{ Old = 'Native/UI/NativeSettingsActionButton.cs'; New = 'Native/Controls/NativeSettingsActionButton.cs'; Sha256 = 'DA6F7CAF41C4DFDF8745401CE40CA46B3A22FFA75C8C17A934F947FE934FB633' }
    @{ Old = 'Native/UI/NativeUiHitTest.cs'; New = 'Native/Controls/NativeUiHitTest.cs'; Sha256 = 'E6049C661E6376EDA0105953D58B7A814201585113BB92A635B39978DBE469D9' }
    @{ Old = 'Native/UI/NativeUiPackageLoader.cs'; New = 'Native/Widgets/NativeUiPackageLoader.cs'; Sha256 = 'E9F0B733C5B4EDCA2712B169A93DE6BEA8EA03146265D1829E946A7F802B5D37' }
    @{ Old = 'Native/UI/StableNativeAction.cs'; New = 'Native/Controls/StableNativeAction.cs'; Sha256 = 'CAA7C68B00C629387275C5B6FBC74A83A6211928691FF9F8F6FFBBBA3C25A3B3' }
    @{ Old = 'Native/UI/StatisticsHistoryView.cs'; New = 'Native/Widgets/StatisticsHistoryView.cs'; Sha256 = '41DA2F755FF9743FF4F27D21775754590A29984286A54135ED05614242B8FEB5' }
    @{ Old = 'Game/HeroPreviewOrdering.cs'; New = ''; Sha256 = '59EBCF4588F0E53D751717AB2A126C55C84D97CE31E38FE6B6A6C1DC75A14591' }
    @{ Old = 'Media/PngFiles.cs'; New = ''; Sha256 = '0EFCB13042A71551F1920198A3DD4C09E8EA338E6B370D67D61C60A3BDE0AB86' }
    @{ Old = 'Replay/ReplayArchivePolicy.cs'; New = ''; Sha256 = '0EFCB13042A71551F1920198A3DD4C09E8EA338E6B370D67D61C60A3BDE0AB86' }
    @{ Old = 'Statistics/AchievementPresentation.cs'; New = ''; Sha256 = '1A642AA556ACC581EC997EC6B72CCAECBD117BBC9A6330C80369A36F758CC939' }

)

$backup = Join-Path (Split-Path $root -Parent) ("AstralParty.Modding.SDK-legacy-backup-v0.12.0-" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$blocked = 0
$ready = 0
foreach ($item in $legacy) {
    $oldPath = Join-Path $root $item.Old
    if (-not (Test-Path -LiteralPath $oldPath -PathType Leaf)) {
        continue
    }
    if ($item.New) {
        $newPath = Join-Path $root $item.New
        if (-not (Test-Path -LiteralPath $newPath -PathType Leaf)) {
            Write-Warning "New target is missing; not moving old source: $($item.Old) -> $($item.New)"
            $blocked++
            continue
        }
    }
    $actual = (Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
    if ($actual -ne $item.Sha256) {
        Write-Warning "Locally modified or line endings differ; review manually, preserving your changes: $($item.Old)"
        $blocked++
        continue
    }
    $ready++
    if (-not $Apply) {
        Write-Host "[Preview] $($item.Old)"
        continue
    }
    $destination = Join-Path $backup $item.Old
    $parent = Split-Path $destination -Parent
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    Move-Item -LiteralPath $oldPath -Destination $destination
    Write-Host "[Backed up] $($item.Old)"
}
Write-Host "Legacy files ready: $ready; manual review needed: $blocked; apply: $Apply"
if ($Apply -and $ready -gt 0) {
    Write-Host "Backup folder: $backup"
}
if ($blocked -gt 0) {
    Write-Warning "Some old files remain and can cause duplicate C# definitions. Review the warnings before building."
}
