using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BluetoothTeste.Contrato;
using Xamarin.Forms;

namespace BluetoothTeste
{
    public partial class MainPage
    {
        public static BindableProperty ListaInformacoesProperty = BindableProperty.Create(nameof(ListaInformacoes), typeof(ObservableCollection<Mensagem>), typeof(MainPage), new ObservableCollection<Mensagem>());
        public ObservableCollection<Mensagem> ListaInformacoes
        {
            get => (ObservableCollection<Mensagem>)GetValue(ListaInformacoesProperty);
            set => SetValue(ListaInformacoesProperty, value);
        }
        public static BindableProperty BotaoConectarHabilitadoProperty = BindableProperty.Create(nameof(BotaoConectarHabilitado), typeof(bool), typeof(MainPage), true);
        public bool BotaoConectarHabilitado
        {
            get => (bool)GetValue(BotaoConectarHabilitadoProperty);
            set => SetValue(BotaoConectarHabilitadoProperty, value);
        }
        public static BindableProperty BotaoCancelarHabilitadoProperty = BindableProperty.Create(nameof(BotaoCancelarHabilitado), typeof(bool), typeof(MainPage));
        public bool BotaoCancelarHabilitado
        {
            get => (bool)GetValue(BotaoCancelarHabilitadoProperty);
            set => SetValue(BotaoCancelarHabilitadoProperty, value);
        }

        private IServicoBluetooth ServicoBluetooth { get; }
        private CancellationTokenSource TokenCancelamento { get; set; }

        public MainPage()
        {
            InitializeComponent();

            ServicoBluetooth = DependencyService.Get<IServicoBluetooth>();
            ServicoBluetooth.AcaoMensagem = async x => await Mensagem(x).ConfigureAwait(false);

            TokenCancelamento = new CancellationTokenSource();

            BotaoConectar_Clicked(null, null);

            BindingContext = this;
        }

        private async void BotaoConectar_Clicked(object sender, EventArgs e)
        {
            BotaoConectarHabilitado = false;
            BotaoCancelarHabilitado = true;

            ListaInformacoes.Clear();

            await ServicoBluetooth.ExecutarAsync(10, 2, TokenCancelamento).ConfigureAwait(true);

            await Mensagem("tarefa concluida").ConfigureAwait(false);

            var texto = ListaInformacoes.Where(x => x != null).Select(x => $"{x.Hora}: {x.Texto}").Aggregate((p, s) => $"{p}\n{s}");

            await Xamarin.Essentials.Clipboard.SetTextAsync(texto).ConfigureAwait(false);

            BotaoConectarHabilitado = true;
            BotaoCancelarHabilitado = false;
        }

        private async void BotaoCancelar_Clicked(object sender, EventArgs e)
        {
            if (TokenCancelamento.IsCancellationRequested)
            {
                BotaoConectarHabilitado = true;
                BotaoCancelarHabilitado = false;
                TokenCancelamento = new CancellationTokenSource();

                await Mensagem("tarefa criada!").ConfigureAwait(false);
            }
            else
            {
                TokenCancelamento.Cancel(true);

                await Mensagem("tarefa cancelada!").ConfigureAwait(false);
            }
        }

        private async Task Mensagem(string texto, int tentativa = 1) => await Device.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                ListaInformacoes.Insert(0, new Mensagem(DateTime.Now.ToString("mm:ss:ffff"), texto));
                Lista.ScrollTo(0, 0, ScrollToPosition.MakeVisible, false);
            }
            catch (Exception ex)
            {
                tentativa++;

                if (tentativa < 3)
                {
                    await Mensagem($"[{nameof(MainPage)}.{nameof(Mensagem)}] {ex.Message}").ConfigureAwait(false);
                    await Mensagem(texto).ConfigureAwait(false);
                }
            }
        }).ConfigureAwait(false);
    }
}
