param([ValidateSet('build','play','edit')][string]$Mode='play',[string]$Filter='')
$ErrorActionPreference='Stop'
$env:CELLDEFENSE_CHAPTER_TWO_PROJECT='E:/unity/CellDefense/Logs/CodexTradingTests'
$env:CELLDEFENSE_VALIDATION_LOG_DIR='E:/unity/CellDefense/Logs/ChapterThree20261029'
$env:CELLDEFENSE_BUILD_METHOD='ChapterThreeContentBuilder.Build'
$env:CELLDEFENSE_CHAPTER_TWO_CAPTURE='E:/unity/CellDefense/Assets/_Project/Art/Sprites/细胞绘制/第三章接入_2026-10-29_v1/验证'
if($Filter){$env:CELLDEFENSE_CHAPTER_TWO_FILTER=$Filter}else{Remove-Item Env:CELLDEFENSE_CHAPTER_TWO_FILTER -ErrorAction SilentlyContinue}
python (Join-Path $PSScriptRoot '../ChapterTwo/validate.py') $Mode
