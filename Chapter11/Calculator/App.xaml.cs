namespace Calculator
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnHandlerChanging(HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);
            MainPage = args.NewHandler.MauiContext.Services.GetService<MainPage>();
        }
    }
}