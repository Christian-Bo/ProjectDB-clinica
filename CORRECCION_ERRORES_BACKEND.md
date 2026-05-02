# Correccion aplicada al backend

## Problema detectado

El proyecto no compilaba porque existian dos archivos declarando las mismas excepciones dentro del namespace `Clinica.Application.Exceptions`:

- `src/Clinica.Application/Exceptions/AppExceptions.cs`
- `src/Clinica.Application/Exceptions/BusinessException.cs`

Ambos contenian las clases:

- `BusinessException`
- `ConflictException`
- `NotFoundException`

Eso producia errores CS0101 y CS0111 durante `dotnet build`.

## Correccion realizada

1. Se dejo una sola fuente de verdad para las excepciones en:
   `src/Clinica.Application/Exceptions/AppExceptions.cs`

2. Se elimino el archivo duplicado:
   `src/Clinica.Application/Exceptions/BusinessException.cs`

3. Se estandarizo el constructor de las excepciones asi:

```csharp
new BusinessException(message, code)
new ConflictException(message, code)
new NotFoundException(message)
```

4. Se corrigieron llamadas en:

- `src/Clinica.Infrastructure/Services/CitasService.cs`
- `src/Clinica.Infrastructure/Services/PacientesService.cs`

Antes algunas llamadas enviaban `(codigo, mensaje)`, lo cual podia hacer que el API respondiera con el codigo como mensaje y el mensaje como codigo.

## Comandos recomendados despues de extraer este ZIP

```bat
dotnet clean
dotnet restore
dotnet build
```

Si compila correctamente, puedes ejecutar el API con:

```bat
dotnet run --project src\Clinica.API\Clinica.API.csproj
```

## Segunda corrección - error SpResult no encontrado

Después de corregir las excepciones duplicadas, la compilación avanzó hasta `Clinica.Infrastructure` y falló porque `CitasRepository` y `PacientesRepository` usaban `SpResult`, `_executor.ExecuteAsync`, `_executor.QueryAsync` y `_executor.QuerySingleAsync`, pero esos elementos no existían en `SqlExecutor`.

Se corrigió lo siguiente:

1. Se agregó `src/Clinica.Infrastructure/Database/SpResult.cs`.
2. Se agregaron a `SqlExecutor` los métodos:
   - `ExecuteAsync(...)`
   - `QueryAsync<T>(...)`
   - `QuerySingleAsync<T>(...)`
3. Se estandarizó la lectura del resultado de Stored Procedures con columnas:
   - `HttpStatus`
   - `Codigo`
   - `Mensaje`
   - `EntityId`, `Id`, `CitaId`, `PacienteId`, etc.
4. Se corrigió `PacientesService` para obtener el `PacienteId` desde `result.EntityId` en lugar de depender de partir el texto de `Codigo`.
5. Se completó `DependencyInjection.cs` registrando repositorios y servicios que ya existen en el proyecto:
   - Citas
   - Pacientes
   - Consultas
   - Ordenes
   - Recetas
   - Auth
   - Health
   - TicketQueue

Comandos recomendados después de extraer esta versión:

```bat
dotnet clean
dotnet restore
dotnet build
```

Si compila correctamente, ejecutar:

```bat
dotnet run --project src\Clinica.API\Clinica.API.csproj
```
