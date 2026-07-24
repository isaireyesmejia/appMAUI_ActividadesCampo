# 📱 Ágaveros Actividades - Documentación de Arquitectura

## 📋 Descripción General

App de .NET MAUI para registrar actividades de campo con soporte:
- ✅ Almacenamiento local con SQLite
- ✅ Sincronización offline-first con API REST
- ✅ Patrón MVVM
- ✅ Compatible con Android, iOS, macOS y Windows

---

## 📁 Estructura de Carpetas

```
agaverosActividades/
├── 📁 Models/                     # Modelos de datos
│   ├── Actividad.cs              # Modelo de actividades
│   └── Usuario.cs                # Modelo de usuarios
│
├── 📁 ViewModels/                # Lógica de presentación (MVVM)
│   ├── BaseViewModel.cs          # Clase base con INotifyPropertyChanged
│   └── ActivitiesViewModel.cs    # ViewModel para lista de actividades
│
├── 📁 Views/                      # Páginas XAML
│   ├── ActivitiesPage.xaml       # Página principal
│   └── ActivitiesPage.xaml.cs    # Code-behind
│
├── 📁 Services/                   # Servicios de negocio
│   ├── IActivityService.cs       # Interfaz de servicios de actividades
│   ├── ActivityService.cs        # Implementación con API + Storage
│   ├── IStorageService.cs        # Interfaz de almacenamiento local
│   └── StorageService.cs         # Implementación con SQLite
│
├── 📁 Converters/                 # Convertidores XAML
│   └── ValueConverters.cs        # Convertidores personalizados
│
├── 📁 Constants/                  # Constantes de la aplicación
│   ├── AppConstants.cs           # Configuración general
│   └── ApiEndpoints.cs           # URLs de API
│
├── 📁 Helpers/                    # Funciones auxiliares
│   └── ValidationHelper.cs       # Validaciones comunes
│
├── 📁 Resources/                  # Recursos visuales
│   ├── Styles/
│   ├── Fonts/
│   ├── Images/
│   └── AppIcon/
│
├── 📁 Platforms/Android/          # Configuración específica Android
│   ├── MainActivity.cs
│   ├── MainApplication.cs
│   └── Resources/
│
├── App.xaml / App.xaml.cs        # Configuración app global
├── AppShell.xaml / AppShell.xaml.cs  # Navegación
├── MauiProgram.cs                 # Inyección de dependencias
└── agaverosActividades.csproj    # Archivo de proyecto
```

---

## 🏗️ Patrón MVVM

### Flujo de datos
```
View (XAML)
	↓ (Binding)
ViewModel (C#)
	↓ (ICommand/INotifyPropertyChanged)
Model + Services
	↓ (API/Storage)
Datos (API REST / SQLite)
```

### Ejemplo de uso:

**View (ActivitiesPage.xaml):**
```xml
<Button Command="{Binding AgregarActividadCommand}" Text="+ Agregar"/>
<CollectionView ItemsSource="{Binding Actividades}"/>
```

**ViewModel (ActivitiesViewModel.cs):**
```csharp
public IAsyncRelayCommand AgregarActividadCommand { get; }
public ObservableCollection<Actividad> Actividades { get; set; }
```

---

## 🔄 Sincronización Offline-First

### Estrategia:
1. **Crear/Actualizar**: Se guarda localmente primero, luego se sincroniza
2. **Lectura**: Si hay internet, obtiene del API; si no, usa storage local
3. **Background**: Se puede ejecutar sincronización periódica

### Flujo:
```
Usuario crea actividad (Offline)
	↓
Guardado en SQLite localmente
	↓ (Si hay conexión)
Se envía al API REST
	↓
Se marca como "Sincronizado"
```

### Usando el servicio:
```csharp
// Crear actividad (automáticamente se maneja offline)
var actividad = new Actividad { Nombre = "Campo A" };
await _activityService.CrearActividadAsync(actividad);

// Sincronizar nuevamente
await _activityService.SincronizarActividadesAsync();
```

---

## 📦 Servicios

### IActivityService
- `ObtenerActividadesAsync()` - Obtiene todas las actividades (API + local)
- `CrearActividadAsync()` - Crea actividad (offline-first)
- `ActualizarActividadAsync()` - Actualiza actividad
- `EliminarActividadAsync()` - Elimina actividad
- `SincronizarActividadesAsync()` - Sincroniza cambios pendientes
- `ObtenerActividadesNoSincronizadasAsync()` - Actividades sin sincronizar

### IStorageService
- Manejo de SQLite
- Persistencia local de datos
- Operaciones CRUD

---

## ⚙️ Configuración

### MauiProgram.cs
Registra todos los servicios, ViewModels y páginas:

```csharp
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IActivityService, ActivityService>();
builder.Services.AddSingleton<ActivitiesViewModel>();
builder.Services.AddSingleton<ActivitiesPage>();
```

### Configurar API
En `Constants/AppConstants.cs`:
```csharp
public const string ApiBaseUrl = "https://tu-api-url.com/";
```

---

## 📱 Para Agregar Nuevas Funcionidades

### 1. Agregar modelo:
```csharp
// Models/Nuevo.cs
public class Nuevo
{
	public int Id { get; set; }
	public string Nombre { get; set; }
}
```

### 2. Agregar interfaz de servicio:
```csharp
// Services/INuevoService.cs
public interface INuevoService
{
	Task<List<Nuevo>> ObtenerAsync();
}
```

### 3. Implementar servicio:
```csharp
// Services/NuevoService.cs
public class NuevoService : INuevoService
{
	// Implementación
}
```

### 4. Crear ViewModel:
```csharp
// ViewModels/NuevosViewModel.cs
public class NuevosViewModel : BaseViewModel
{
	private readonly INuevoService _service;
	// Lógica
}
```

### 5. Crear Vista:
```xaml
<!-- Views/NuevosPage.xaml -->
<ContentPage>
	<CollectionView ItemsSource="{Binding Nuevos}"/>
</ContentPage>
```

### 6. Registrar en MauiProgram:
```csharp
builder.Services.AddSingleton<INuevoService, NuevoService>();
builder.Services.AddSingleton<NuevosViewModel>();
builder.Services.AddSingleton<NuevosPage>();
```

---

## 🔌 Dependencias Requeridas

Asegúrate de tener en tu `.csproj`:

```xml
<ItemGroup>
	<PackageReference Include="sqlite-net-pcl" Version="1.8.116" />
	<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
</ItemGroup>
```

---

## 🚀 Próximos Pasos

- [ ] Agregar página de crear/editar actividad
- [ ] Implementar autenticación de usuario
- [ ] Agregar geolocalización con GPS
- [ ] Agregar cámara para fotos
- [ ] Implementar estadísticas
- [ ] Agregar notificaciones locales
- [ ] Mejorar UI/UX

---

## 📞 Contacto

Para preguntas sobre la arquitectura, revisa esta documentación o contacta al equipo de desarrollo.
