# xliff-localizer
A .NET CLI tool that helps localize xliff files. This will take the input XLF file and it will translate all the entries which have `state="new"`.

### Installation

```
dotnet tool install --global Xliff-Localizer
```

### Usage

```
xliff-localizer
  --file <path to .xlf file> 
  --apikey <your Azure Translator API key> 
  --region <your Azure Translator region> 
  --from <source language>
  --to <target language>
```

### Example

```
xliff-localizer
  --file "C:\Users\kid_j\source\repos\ambie\src\AmbientSounds.Uwp\MultilingualResources\AmbientSounds.Uwp.sv-SE.xlf"
  --apikey <your Azure Translator API key> 
  --region "westus"
  --from "en"
  --to "sv"
```
