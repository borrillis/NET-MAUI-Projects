using System.Windows.Input;

namespace SticksAndStones.Controls;

public partial class ActivityButton : Frame
{
    public ActivityButton()
    {
        InitializeComponent();
    }

    public event EventHandler<EventArgs> Tapped;

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        propertyName: nameof(Command),
        returnType: typeof(ICommand),
        declaringType: typeof(ActivityButton),
        defaultBindingMode: BindingMode.TwoWay);

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set { SetValue(CommandProperty, value); }
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        propertyName: nameof(FontFamily),
        returnType: typeof(string),
        declaringType: typeof(ActivityButton),
        defaultValue: "",
        defaultBindingMode: BindingMode.TwoWay);

    public string FontFamily
    {
        get => (string)GetValue(Label.FontFamilyProperty);
        set { SetValue(Label.FontFamilyProperty, value); }
    }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize),
        typeof(double),
        typeof(ActivityButton),
        Device.GetNamedSize(NamedSize.Small, typeof(Label)),
        BindingMode.TwoWay);

    public double FontSize
    {
        set { SetValue(FontSizeProperty, value); }
        get { return (double)GetValue(FontSizeProperty); }
    }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        propertyName: nameof(Text),
        returnType: typeof(string),
        declaringType: typeof(ActivityButton),
        defaultValue: "",
        defaultBindingMode: BindingMode.TwoWay);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set { SetValue(TextProperty, value); }
    }

    public static readonly BindableProperty LoadingTextProperty = BindableProperty.Create(
        propertyName: nameof(Text),
        returnType: typeof(string),
        declaringType: typeof(ActivityButton),
        defaultValue: "Please wait...",
        defaultBindingMode: BindingMode.OneWay);

    public string LoadingText
    {
        get => (string)GetValue(LoadingTextProperty);
        set { SetValue(LoadingTextProperty, value); }
    }

    public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
       propertyName: nameof(IsRunning),
       returnType: typeof(bool),
       declaringType: typeof(ActivityButton),
       defaultValue: false,
       propertyChanged: IsRunningPropertyChanged);

    private static void IsRunningPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var controls = (ActivityButton)bindable;
        if (newValue != null)
        {
            bool isRunning = (bool)newValue;
            if (isRunning)
                controls.buttonLabel.Text = controls.LoadingText;
            else
                controls.buttonLabel.Text = controls.Text;
        }
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set { SetValue(IsRunningProperty, value); }
    }

    private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        Tapped?.Invoke(sender, e);
    }
}
