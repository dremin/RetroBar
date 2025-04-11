function Get-LanguageCode {
	param (
		[string]$LanguageName
	)

	try {
		$cultures = [System.Globalization.CultureInfo]::GetCultures('AllCultures') | Where-Object { $_.DisplayName -like "*$LanguageName*" -or $_.NativeName -like "*$LanguageName*" }
		if ($cultures) {
			$exactMatch = $cultures | Where-Object { $_.DisplayName -eq $LanguageName -or $_.NativeName -eq $LanguageName }
			if ($exactMatch) {
				return $exactMatch[0].Name
			}
			return $cultures[0].TwoLetterISOLanguageName
		}
	} catch {
		Write-Error "Error: $_"
		# Fall back to a sanitized name
	}
	return ($LanguageName -replace '[^a-zA-Z0-9]', '').ToLower()
}

function Get-ComponentName {
	param (
		[string]$FileName
	)

	$languageName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
	$languageCode = Get-LanguageCode -LanguageName $languageName
	return $languageCode.ToLower().Replace('-', '_')
}

function Get-XamlFiles {
	param (
		[string]$Directory,
		[string]$ExcludeFileName
	)
	try {
		return Get-ChildItem -Path $Directory -Filter "*.xaml" | Where-Object { $_.Name -ne $ExcludeFileName } | Select-Object -ExpandProperty Name | Sort-Object
	} catch {
		Write-Error "Error reading directory: $_"
		return @()
	}
}

function Get-InnoSetupLanguageSections {
	param (
		[string]$LanguagesDir
	)

	$languageFiles = Get-XamlFiles -Directory $LanguagesDir -ExcludeFileName "English.xaml"

	# Generate components section
	$componentsSection = "; Components - Languages`n"
	$componentsSection += "Name: `"languages`"; Description: `"{cm:LanguagesComponentName}`"; Types: full compact custom`n"
	$componentsSection += "Name: `"languages\en`"; Description: `"English`"; Types: full compact custom; Flags: fixed`n"

	# Generate files section
	$filesSection = "; Files - Languages`n"

	foreach ($file in $languageFiles) {
		$name = [System.IO.Path]::GetFileNameWithoutExtension($file)
		$componentName = Get-ComponentName -FileName $file

		$componentsSection += "Name: `"languages\$componentName`"; Description: `"$name`"; Types: full custom`n"
		$filesSection += "Source: `"{#LanguagePath}\$file`"; DestDir: `"{app}\Languages`"; Components: `"languages\$componentName`"; Flags: ignoreversion`n"
	}

	return $componentsSection, $filesSection
}

function Get-XamlThemeReferencedFiles {
	param(
		[string]$XamlFile
	)

	([xml](Get-Content $XamlFile)).SelectNodes("//*[@UriSource]") | ForEach-Object {
		$_.UriSource.Replace("../", "").Replace("/", "\").Replace("Resources\", "{#ResourcesPath}\")
	}
}

function Get-XamlThemeReferencedDictionaryFiles {
	param(
		[string]$XamlFile
	)
	
	([xml](Get-Content $XamlFile)).SelectNodes("//*[@Source]") | ForEach-Object {
		$_.Source
	}
}

$codeSection = ""

function Get-ThemeEntry {
	param(
		[string]$ThemesDir,
		[string]$File,
		[string]$GroupName
	)
	$name = [System.IO.Path]::GetFileNameWithoutExtension($File)
	$componentName = Get-ComponentName -FileName $File
	$componentsTypes = if ($componentName -eq "windows9598") { "full compact custom" } else { "full custom" }
	
	$componentLine = "Name: `"themes\$GroupName\$componentName`"; Description: `"$name`"; Types: $componentsTypes`n"
	$filesLine = "Source: `"{#ThemePath}\$File`"; DestDir: `"{app}\Themes`"; Components: `"themes\$GroupName\$componentName`"; Flags: ignoreversion`n"

	$dependsOnArray = @()
	
	# Process referenced dictionary files
	foreach ($referencedDictionaryFile in Get-XamlThemeReferencedDictionaryFiles -XamlFile "$ThemesDir\$File") {
		$dependsOnGroupName = Get-ThemeCategorySingle -ThemeFile $referencedDictionaryFile
		$dependsOnComponentName = Get-ComponentName -FileName $referencedDictionaryFile
		$dependsOnArray += "themes\$dependsOnGroupName\$dependsOnComponentName"
	}

	$dependsOnString = $dependsOnArray -join "', '"
	if ($dependsOnString) {
		$global:codeSection += "    AddDependencies('themes\$GroupName\$componentName', ['$dependsOnString']);`n"
	}
	
	# Process additional referenced files
	foreach ($referencedFile in Get-XamlThemeReferencedFiles -XamlFile "$ThemesDir\$File") {
		$filesLine += "Source: `"$referencedFile`"; DestDir: `"{app}\Resources`"; Components: `"themes\$GroupName\$componentName`"; Flags: ignoreversion`n"
	}
	
	return ,@($componentLine, $filesLine)
}

function Get-ThemeGroupName {
	param(
		[string]$ThemeName
	)
	if ($ThemeName -like "*System*") {
		return "system"
	}
	elseif ($ThemeName -eq "Windows 95-98" -or $ThemeName -eq "Windows Me" -or $ThemeName -eq "Windows 2000" -or $ThemeName -like "*Classic*") {
		return "classic"
	}
	elseif ($ThemeName -like "Windows XP*") {
		return "xp"
	}
	elseif ($ThemeName -like "Windows Vista*") {
		return "vista"
	}
	else {
		return "other"
	}
}

function Get-ThemeCategorySingle {
	param(
		[string]$ThemeFile
	)
	$name = [System.IO.Path]::GetFileNameWithoutExtension($ThemeFile)
	return Get-ThemeGroupName -ThemeName $name
}

function Get-ThemeCategory {
	param(
		[string[]]$ThemeFiles
	)
	$themeGroups = @{}
	foreach ($file in $ThemeFiles) {
		$group = Get-ThemeCategorySingle -ThemeFile $file
		if (-not $themeGroups.ContainsKey($group)) {
			$themeGroups[$group] = @()
		}
		$themeGroups[$group] += $file
	}
	return $themeGroups
}

function Get-InnoSetupThemeSections {
	param (
		[string]$ThemesDir
	)

	$themeFiles = Get-XamlFiles -Directory $ThemesDir -ExcludeFileName "System.xaml"

	# Generate components and files section headers
	$componentsSection = "; Components - Themes`n"
	$componentsSection += "Name: `"themes`"; Description: `"{cm:ThemesComponentName}`"; Types: full compact custom`n"
	$componentsSection += "Name: `"themes\system`"; Description: `"System`"; Types: full compact custom; Flags: checkablealone fixed`n"

	$filesSection = "; Files - Themes`n"

	# Define group descriptions
	$groupDescriptions = @{
		"classic" = "Classic Windows Themes"
		"xp"      = "Windows XP Themes"
		"vista"   = "Windows Vista Themes"
		"other"   = "Other Themes"
	}

	# Define ordered groups
	$orderedGroups = @("system", "classic", "xp", "vista", "other")
	$themeGroups = Get-ThemeCategory -ThemeFiles $themeFiles

	# Process theme groups in a specific order
	foreach ($groupName in $orderedGroups) {
		$categoryAdded = ($groupName -eq "system")  # System category already added above.
		if ($themeGroups.ContainsKey($groupName)) {
			foreach ($file in $themeGroups[$groupName]) {
				if (-not $categoryAdded -and $groupName -ne "system") {
					$componentsSection += "Name: `"themes\$groupName`"; Description: `"$($groupDescriptions[$groupName])`"; Types: full compact custom`n"
					$categoryAdded = $true
				}
				$entry = Get-ThemeEntry -ThemesDir $ThemesDir -File $file -GroupName $groupName
				$componentsSection += $entry[0]
				$filesSection += $entry[1]
			}
		}
	}

	return $componentsSection, $filesSection
}

# Main execution
$languagesDir = "RetroBar\Languages"
$themesDir = "RetroBar\Themes"
$outputFile = "installer.iss"

$languageComponentsSection, $languageFilesSection = Get-InnoSetupLanguageSections -LanguagesDir $languagesDir
$themeComponentsSection, $themeFilesSection = Get-InnoSetupThemeSections -ThemesDir $themesDir

# Create UTF-8 encoding without BOM
$utf8WithoutBomEncoding = New-Object System.Text.UTF8Encoding $true
$fileContent = if (Test-Path $outputFile) { Get-Content $outputFile -Raw -Encoding utf8 } else { "" }

function Update-FileSection {
	param (
		[string]$FileContent,
		[string]$SectionMarker,
		[string]$SectionContent
	)

	if ($FileContent -match [regex]::Escape($SectionMarker)) {
		$beforeSection = $FileContent.Substring(0, $FileContent.IndexOf($SectionMarker) + $SectionMarker.Length)
		$afterSectionMatch = [regex]::Match($FileContent.Substring($beforeSection.Length), "(\r?\n){2,}")

		if ($afterSectionMatch.Success) {
			$afterSection = $FileContent.Substring($beforeSection.Length + $afterSectionMatch.Index + $afterSectionMatch.Length)
			return $beforeSection + "`n" + $SectionContent + "`n" + $afterSection
		} else {
			return $beforeSection + "`n" + $SectionContent
		}
	}

	return $FileContent
}

# Process sections and update file content
$fileContent = Update-FileSection -FileContent $fileContent -SectionMarker "; [auto-generated components section]" -SectionContent ($languageComponentsSection + $themeComponentsSection)
$fileContent = Update-FileSection -FileContent $fileContent -SectionMarker "; [auto-generated files section]" -SectionContent ($languageFilesSection + $themeFilesSection)
$fileContent = Update-FileSection -FileContent $fileContent -SectionMarker "// [auto-generated code section]" -SectionContent $codeSection

# Write final content to file
[System.IO.File]::WriteAllText($outputFile, $fileContent, $utf8WithoutBomEncoding)

Write-Host "Sections have been generated and saved to '$outputFile'"

# Also output to console
Write-Host "`n=== COMPONENTS SECTION ==="
Write-Host $languageComponentsSection
Write-Host $themeComponentsSection

Write-Host "`n=== FILES SECTION ==="
Write-Host $languageFilesSection
Write-Host $themeFilesSection

Write-Host "`n=== CODE SECTION ==="
Write-Host $codeSection