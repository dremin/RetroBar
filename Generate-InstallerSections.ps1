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

function Get-InnoSetupLanguageSections {
	param (
		[string]$LanguagesDir
	)

	try {
		$languageFiles = Get-ChildItem -Path $LanguagesDir -Filter "*.xaml" | Where-Object { $_.Name -ne "English.xaml" } | Select-Object -ExpandProperty Name | Sort-Object
	} catch {
		return "Error reading directory: $_", ""
	}

	# Generate components section
	$componentsSection = "; Components - Languages`n"
	$componentsSection += "Name: `"languages`"; Description: `"{cm:LanguagesComponentName}`"; Types: full compact custom; Flags: fixed`n"
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

# Main execution
$languagesDir = "RetroBar\Languages"
$outputFile = "installer.iss"

$languageComponentsSection, $languageFilesSection = Get-InnoSetupLanguageSections -LanguagesDir $languagesDir

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
$fileContent = Update-FileSection -FileContent $fileContent -SectionMarker "; [auto-generated components section]" -SectionContent ($languageComponentsSection)
$fileContent = Update-FileSection -FileContent $fileContent -SectionMarker "; [auto-generated files section]" -SectionContent ($languageFilesSection)

# Write final content to file
[System.IO.File]::WriteAllText($outputFile, $fileContent, $utf8WithoutBomEncoding)

Write-Host "Sections have been generated and saved to '$outputFile'"

# Also output to console
Write-Host "`n=== COMPONENTS SECTION ==="
Write-Host $languageComponentsSection

Write-Host "`n=== FILES SECTION ==="
Write-Host $languageFilesSection