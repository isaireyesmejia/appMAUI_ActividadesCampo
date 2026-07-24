using agaverosActividades.Services;

namespace agaverosActividades
{
    public partial class App : Application
    {
        public App(IDatabaseService databaseService)
        {
            InitializeComponent();

            // Se dispara sin await (un constructor no puede ser async). Cualquier
            // página o servicio que toque SQLite debe esperar
            // IDatabaseService.ListoAsync antes de su primera consulta — ver el
            // patrón en el Paso 2/3 cuando conectemos el outbox.
            _ = databaseService.InicializarAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}