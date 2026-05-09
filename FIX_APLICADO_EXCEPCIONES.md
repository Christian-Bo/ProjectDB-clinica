# Fix aplicado - Excepciones duplicadas

Se corrigio el error del CI donde el namespace `Clinica.Application.Exceptions` tenia definiciones duplicadas de:

- BusinessException
- ConflictException
- NotFoundException

## Archivos modificados

- `src/Clinica.Application/Exceptions/AppExceptions.cs`
- `src/Clinica.Application/Exceptions/BusinessException.cs`
- `src/Clinica.Application/Exceptions/ConflictException.cs`
- `src/Clinica.Application/Exceptions/NotFoundException.cs`

## Validacion recomendada

Desde la raiz del proyecto ejecutar:

```powershell
dotnet clean
dotnet restore
dotnet build -c Release
```

Y para verificar que no existan duplicados:

```powershell
Get-ChildItem .\src\Clinica.Application -Recurse -Filter *.cs |
Select-String -Pattern "class\s+(BusinessException|ConflictException|NotFoundException)" |
Select-Object Path, LineNumber, Line
```

El resultado debe mostrar solo una definicion por clase.
