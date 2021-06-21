using Android.App;
using Android.Content;
using Android.OS;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content.PM;
using Android.Media;
using BluetoothTeste.Contrato;
using Java.Util;
using Plugin.CurrentActivity;
using Xamarin.Forms;
using Encoding = System.Text.Encoding;
using Stream = System.IO.Stream;

[assembly: Dependency(typeof(BluetoothTeste.Droid.ServicoBluetooth))]
namespace BluetoothTeste.Droid
{
    public sealed class ServicoBluetooth : Activity, IServicoBluetooth
    {
        public Action<string> AcaoMensagem { get; set; }

        private static BluetoothDevice DispositivoSelecionado { get; set; }
        private static readonly EventWaitHandle EventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        public async Task ExecutarAsync(int tempoMaximoExecucao, int quantidadeMaximaTentativas, CancellationTokenSource tokenCancelamento)
        {
            var adaptador = BluetoothAdapter.DefaultAdapter;
            var dispositivo = await SelecionarDispositivoAsync().ConfigureAwait(true);
            var listaPerfis = Enum.GetValues(typeof(ProfileType)).OfType<ProfileType>().ToList();

            if (dispositivo == null)
            {
                AcaoMensagem?.Invoke("Não foi possível selecionar o dispositivo, tente novamente!");

                return;
            }

            AcaoMensagem?.Invoke("------------------------");
            AcaoMensagem?.Invoke($"{nameof(dispositivo.Name)}: {dispositivo.Name}");
            AcaoMensagem?.Invoke($"{nameof(dispositivo.Address)}: {dispositivo.Address}");
            AcaoMensagem?.Invoke($"{nameof(dispositivo.BondState)}: {dispositivo.BondState}");
            AcaoMensagem?.Invoke($"{nameof(dispositivo.Type)}: {dispositivo.Type}");
            AcaoMensagem?.Invoke($"{nameof(dispositivo.BluetoothClass.DeviceClass)}: {dispositivo.BluetoothClass?.DeviceClass}");
            AcaoMensagem?.Invoke($"{nameof(dispositivo.BluetoothClass.MajorDeviceClass)}: {dispositivo.BluetoothClass?.MajorDeviceClass}");

            var mensagemProfileType = "ProfileType: ";

            for (var index = 0; index < listaPerfis.Count; index++)
            {
                var valor = listaPerfis[index];
                var resultado = adaptador?.GetProfileProxy(CrossCurrentActivity.Current.Activity, new BluetoothProfileServiceListener(dispositivo, AcaoMensagem), valor);

                mensagemProfileType += $"{valor} ({(resultado != null ? Convert.ToString(resultado) : "NULL")})";

                if (index != listaPerfis.Count - 1)
                {
                    mensagemProfileType += " | ";
                }
            }

            AcaoMensagem?.Invoke(mensagemProfileType);
            
            AcaoMensagem.ExecucaoSegura(nameof(dispositivo.CreateBond), () => dispositivo.CreateBond());
            AcaoMensagem.ExecucaoSegura(nameof(dispositivo.SetPin), () => dispositivo.SetPin(Encoding.ASCII.GetBytes("1234")));
            AcaoMensagem.ExecucaoSegura(nameof(dispositivo.SetPairingConfirmation), () => dispositivo.SetPairingConfirmation(true));

            var tipoTransporte = dispositivo.Type == BluetoothDeviceType.Classic ? BluetoothTransports.Bredr: BluetoothTransports.Le;
            var gatt = dispositivo.ConnectGatt(CrossCurrentActivity.Current.Activity, false, new BluetoothCallback(AcaoMensagem), tipoTransporte, ScanSettingsPhy.AllSupported, Handler.CreateAsync(Looper.MainLooper));

            AcaoMensagem.ExecucaoSegura(nameof(gatt.Connect), () => gatt?.Connect());
            AcaoMensagem.ExecucaoSegura(nameof(gatt.DiscoverServices), () => gatt?.DiscoverServices());
            AcaoMensagem?.Invoke($"[{nameof(TentarAsync)}] existem {gatt?.Services?.Count ?? 0} serviços");

            try
            {
                var servicoBluetooth = GetSystemService(BluetoothService);
                var servicoAudio = GetSystemService(AudioService);

                if (servicoBluetooth is BluetoothManager bluetoothManager)
                {
                    var gattServer = bluetoothManager.OpenGattServer(CrossCurrentActivity.Current.Activity, new BluetoothServerCallback(AcaoMensagem));

                    AcaoMensagem.ExecucaoSegura(nameof(gattServer.Connect), () => gattServer?.Connect(dispositivo, false));
                    AcaoMensagem?.Invoke($"[{nameof(TentarAsync)}] existem {gattServer?.Services?.Count ?? 0} serviços do servidor");
                }
                if (servicoAudio is AudioManager audioManager)
                {
                    var modo = audioManager.Mode;

                    AcaoMensagem?.Invoke($"[{nameof(TentarAsync)}]{nameof(audioManager)} está no modo {modo}");
                }
            }
            catch (Exception ex)
            {
                AcaoMensagem?.Invoke($"[{nameof(TentarAsync)}] {ex.Message}");
            }

            var indexServico = 1;
            var totalServico = Utilidade.ListaUuid.Count;

            foreach (var uuid in Utilidade.ListaUuid)
            {
                var uuidConvertido = UUID.FromString(uuid.Value);

                await PrepararAsync(adaptador, dispositivo, $"[{indexServico}/{totalServico}] {uuid.Key}", uuidConvertido, quantidadeMaximaTentativas, tempoMaximoExecucao, tokenCancelamento).ConfigureAwait(true);

                indexServico++;
            }

            if (dispositivo.GetUuids() is ParcelUuid[] listaUuidDispositivo)
            {
                var indexServicoDispositivo = listaUuidDispositivo.Length;

                for (var index = 0; index < listaUuidDispositivo.Length; index++)
                {
                    var uuid = listaUuidDispositivo[0]?.Uuid;

                    if (uuid != null)
                    {
                        await PrepararAsync(adaptador, dispositivo, $"[{index + 1}/{indexServicoDispositivo}]", uuid, quantidadeMaximaTentativas, tempoMaximoExecucao, tokenCancelamento).ConfigureAwait(true);
                    }
                }
            }
        }

        public async Task<BluetoothDevice> SelecionarDispositivoAsync()
        {
            var intent = new Intent(CrossCurrentActivity.Current.Activity, typeof(DevicePickerActivity));

            DispositivoSelecionado = null;
            DevicePickerActivity.AcaoMensagem = AcaoMensagem;

            CrossCurrentActivity.Current.Activity.StartActivity(intent);

            return await Task.Run(() =>
            {
                EventWaitHandle.WaitOne();

                return DispositivoSelecionado;
            }).ConfigureAwait(true);
        }

        public async Task PrepararAsync(BluetoothAdapter adaptador, BluetoothDevice dispositivo, string nome, UUID uuid, int quantidadeMaximaTentativas, int tempoMaximoExecucao, CancellationTokenSource tokenCancelamento)
        {
            BluetoothSocket soqueteCliente = null;
            BluetoothServerSocket servidor = null;

            var contadorTentativas = 0;
            var tokenCancelamentoInterno = tokenCancelamento.IsCancellationRequested ? tokenCancelamento : new CancellationTokenSource(tempoMaximoExecucao * 500);
            var tarefaComplementar = new Func<string, BluetoothSocket, Task>(async (sistema, soquete) =>
            {
                await TentarAsync(sistema, soquete, tempoMaximoExecucao, tokenCancelamentoInterno.Token).ConfigureAwait(true);

                AcaoMensagem?.Invoke($"[{nameof(PrepararAsync)}] {sistema} iniciando tarefa internas");

                try
                {
                    var tipoTransporte = dispositivo.Type == BluetoothDeviceType.Classic ? BluetoothTransports.Bredr : BluetoothTransports.Le;
                    var gatt = dispositivo.ConnectGatt(CrossCurrentActivity.Current.Activity, false, new BluetoothCallback(AcaoMensagem), tipoTransporte, ScanSettingsPhy.AllSupported, Handler.CreateAsync(Looper.MainLooper));

                    AcaoMensagem?.ExecucaoSegura(nameof(gatt.RequestConnectionPriority), () => gatt?.RequestConnectionPriority(GattConnectionPriority.High));
                    AcaoMensagem?.ExecucaoSegura(nameof(gatt.Connect), () => gatt?.Connect());
                    AcaoMensagem?.ExecucaoSegura(nameof(gatt.DiscoverServices), () => gatt?.DiscoverServices());
                    AcaoMensagem?.Invoke($"[{sistema}]: existem {gatt?.Services?.Count ?? 0} serviços");
                }
                catch (Exception ex)
                {
                    AcaoMensagem?.Invoke($"[{sistema}]: {ex.Message}");
                }
            });

            AcaoMensagem?.Invoke("++++++++++++++++++++++++");
            AcaoMensagem?.Invoke($"{nome}: iniciando");

            try
            {
                //var createRfcommSocket = JNIEnv.GetMethodID(dispositivo.Class.Handle, "createRfcommSocket", "(I)Landroid/bluetooth/BluetoothSocket;");
                //var _socket = JNIEnv.CallObjectMethod(dispositivo.Handle, createRfcommSocket, new Android.Runtime.JValue(1));
                //var socket = Java.Lang.Object.GetObject(_socket, JniHandleOwnership.TransferLocalRef);

                soqueteCliente = dispositivo.CreateRfcommSocketToServiceRecord(uuid);
                servidor = adaptador?.ListenUsingRfcommWithServiceRecord($"Servidor - {nome}", uuid);

                AcaoMensagem?.Invoke("cliente e servidor criados");
                //AcaoMensagem?.Invoke("cliente criado");
            }
            catch (Exception ex)
            {
                AcaoMensagem?.Invoke($"[soquete]: {ex.Message}");
            }

            do
            {
                contadorTentativas++;

                if (quantidadeMaximaTentativas > 1)
                {
                    AcaoMensagem?.Invoke($"tentativa {contadorTentativas} começando");
                }

                var tarefas = new[]
                {
                    Task.Run(async () =>
                    {
                        if (servidor == null)
                        {
                            AcaoMensagem?.Invoke($"[{nameof(servidor)}] não foi definido");

                            return;
                        }

                        AcaoMensagem?.Invoke($"[{nameof(servidor)}] {nameof(servidor.AcceptAsync)}");

                        try
                        {
                            var soqueteServidor = await servidor.AcceptAsync(tempoMaximoExecucao * 250).ConfigureAwait(true);

                            await tarefaComplementar(nameof(soqueteServidor), soqueteServidor).ConfigureAwait(true);
                        }
                        catch (Exception ex)
                        {
                            AcaoMensagem?.Invoke($"[{nameof(servidor)}]: {ex.Message}");
                        }

                    }, tokenCancelamentoInterno.Token),
                    Task.Run(async () => await tarefaComplementar(nameof(soqueteCliente), soqueteCliente).ConfigureAwait(true), tokenCancelamentoInterno.Token)
                };

                await Utilidade.ParallelForEachAsync(tarefas, async x => await x.ConfigureAwait(true), 2, AcaoMensagem, tokenCancelamentoInterno.Token).ConfigureAwait(true);

                soqueteCliente?.Close();
                servidor?.Close();

                if (quantidadeMaximaTentativas > 1)
                {
                    AcaoMensagem?.Invoke($"tentativa {contadorTentativas} finalizada");
                }

            } while (contadorTentativas < quantidadeMaximaTentativas);

            AcaoMensagem?.Invoke($"{nome}: finalizado");
        }

        public async Task TentarAsync(string sistema, BluetoothSocket soquete, int tempoMaximoExecucao, CancellationToken token)
        {
            var tarefaSimplificada = new Func<Stream, string, bool, Task>(async (stream, nome, read) =>
            {
                var cabecalho = $"[{sistema}] {nome}.{(read ? nameof(Stream.ReadAsync) : nameof(StreamWriter.WriteAsync))}";

                AcaoMensagem?.Invoke($"{cabecalho} iniciando");

                if (stream == null)
                {
                    AcaoMensagem?.Invoke($"{cabecalho} não foi definido");

                    return;
                }
                if (read && !stream.CanRead || !read && !stream.CanWrite)
                {
                    AcaoMensagem?.Invoke($"{cabecalho} não é possível");

                    return;
                }

                try
                {
                    if (read)
                    {
                        int resultado;
                        var quantidadeRecebida = 0;
                        var buffer = new byte[64];

                        do
                        {
                            //deve ter algum problema com o serviço --> HandsfreeServiceClass
                            resultado = await stream.ReadAsync(buffer, 0, buffer.Length - quantidadeRecebida, token).ConfigureAwait(true);

                            AcaoMensagem?.Invoke($"{cabecalho} {nameof(resultado)}: {resultado}");

                            quantidadeRecebida += resultado;
                        } while (resultado > 0);

                        var mensagem = Encoding.UTF8.GetString(buffer);

                        AcaoMensagem?.Invoke($"{cabecalho} {mensagem}");
                    }
                    else
                    {
                        var mensagem = $"{nome} escrito";
                        var bytes = Encoding.UTF8.GetBytes(mensagem);

                        await stream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(true);

                        AcaoMensagem?.Invoke($"{cabecalho} sucesso");
                    }
                }
                catch (Exception ex)
                {
                    AcaoMensagem?.Invoke($"{cabecalho} {ex.Message}");
                }

                AcaoMensagem?.Invoke($"{cabecalho} finalizado");
            });

            if (soquete == null)
            {
                AcaoMensagem?.Invoke($"[{sistema}] {nameof(soquete)} não foi definido");

                return;
            }

            try
            {
                AcaoMensagem?.Invoke($"[{sistema}] iniciando");

                if (!soquete.IsConnected)
                {
                    AcaoMensagem?.Invoke($"[{sistema}] {nameof(soquete.ConnectAsync)}");

                    await soquete.ConnectAsync().ConfigureAwait(true);
                }

                AcaoMensagem?.Invoke($"[{sistema}] {nameof(soquete.IsConnected)}: {soquete.IsConnected}");

                if (soquete.IsConnected)
                {
                    var tarefas = new[]
                    {
                        tarefaSimplificada(soquete.InputStream, nameof(soquete.InputStream), true),
                        tarefaSimplificada(soquete.InputStream, nameof(soquete.InputStream), false),
                        tarefaSimplificada(soquete.OutputStream, nameof(soquete.OutputStream), true),
                        tarefaSimplificada(soquete.OutputStream, nameof(soquete.OutputStream), false)
                    };

                    await Utilidade.ParallelForEachAsync(tarefas, async x => await x.ConfigureAwait(true), 4, AcaoMensagem, token).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                AcaoMensagem?.Invoke($"[{nameof(TentarAsync)}] {ex.Message}");
            }
        }

        internal class BluetoothCallback : BluetoothGattCallback
        {
            private Action<string> AcaoMensagem { get; }

            public BluetoothCallback(Action<string> acaoMensagem)
            {
                AcaoMensagem = acaoMensagem;
            }

            public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
            {
                base.OnConnectionStateChange(gatt, status, newState);

                AcaoMensagem?.Invoke($"[{nameof(BluetoothCallback)}.{nameof(OnConnectionStateChange)}] {newState}: {status}");
            }
        }

        internal class BluetoothServerCallback : BluetoothGattServerCallback
        {
            private Action<string> AcaoMensagem { get; }

            public BluetoothServerCallback(Action<string> acaoMensagem)
            {
                AcaoMensagem = acaoMensagem;
            }

            public override void OnConnectionStateChange(BluetoothDevice device, ProfileState status, ProfileState newState)
            {
                base.OnConnectionStateChange(device, status, newState);

                AcaoMensagem?.Invoke($"[{nameof(BluetoothServerCallback)}.{nameof(OnConnectionStateChange)}] {newState}: {status}");
            }
        }

        internal class BluetoothProfileServiceListener : Java.Lang.Object, IBluetoothProfileServiceListener
        {
            private Action<string> AcaoMensagem { get; }
            private BluetoothDevice Dispositivo { get; }

            public BluetoothProfileServiceListener(BluetoothDevice dispositivo, Action<string> acaoMensagem)
            {
                Dispositivo = dispositivo;
                AcaoMensagem = acaoMensagem;
            }

            public void OnServiceConnected(ProfileType profile, IBluetoothProfile proxy)
            {
                var listaStatus = Enum.GetValues(typeof(ProfileState)).OfType<ProfileState>().ToArray();
                var listaDispositivosConectados = proxy.ConnectedDevices;
                var statusDispositivo = proxy.GetConnectionState(Dispositivo);
                var listaDispositivos = proxy.GetDevicesMatchingConnectionStates(listaStatus);

                AcaoMensagem?.Invoke($"[{nameof(BluetoothProfileServiceListener)}.{nameof(OnServiceConnected)}] {profile} com parâmetro {nameof(listaDispositivosConectados)}: {listaDispositivosConectados?.Count ?? 0}, {nameof(statusDispositivo)}: {statusDispositivo} e {nameof(listaDispositivos)}: {listaDispositivos?.Count ?? 0}");
            }

            public void OnServiceDisconnected(ProfileType profile)
            {
                AcaoMensagem?.Invoke($"[{nameof(BluetoothProfileServiceListener)}.{nameof(OnServiceDisconnected)}] parâmetro {profile} ");
                //var asda = new MediaPlayer().SetPreferredDevice(new AudioDeviceInfo().Type.)
            }
        }

        [Activity(NoHistory = false, LaunchMode = LaunchMode.Multiple)]
        internal sealed class DevicePickerActivity : Activity
        {
            public static Action<string> AcaoMensagem { get; set; }

            protected override void OnCreate(Bundle savedInstanceState)
            {
                base.OnCreate(savedInstanceState);

                var intent = new Intent("android.bluetooth.devicepicker.action.LAUNCH");

                intent.PutExtra("android.bluetooth.devicepicker.extra.LAUNCH_PACKAGE", CrossCurrentActivity.Current.Activity.PackageName);
                intent.PutExtra("android.bluetooth.devicepicker.extra.DEVICE_PICKER_LAUNCH_CLASS", Java.Lang.Class.FromType(typeof(DevicePickerReceiver)).Name);
                intent.PutExtra("android.bluetooth.devicepicker.extra.NEED_AUTH", false);

                CrossCurrentActivity.Current.Activity.StartActivityForResult(intent, 1);
            }

            // set the handle when the picker has completed and return control straight back to the calling activity
            protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
            {
                AcaoMensagem?.Invoke($"[{nameof(DevicePickerActivity)}.{nameof(OnActivityResult)}] {nameof(requestCode)}: {resultCode}, {nameof(requestCode)}: {requestCode}");

                base.OnActivityResult(requestCode, resultCode, data);

                EventWaitHandle.Set();

                CrossCurrentActivity.Current.Activity.Finish();
            }
        }

        [BroadcastReceiver(Enabled = true)]
        internal sealed class DevicePickerReceiver : BroadcastReceiver
        {
            // receive broadcast if a device is selected and store the device.
            public override void OnReceive(Context context, Intent intent)
            {
                var informacaoExtra = intent?.Extras?.Get("android.bluetooth.device.extra.DEVICE");

                if (informacaoExtra is BluetoothDevice dispositivo)
                {
                    DispositivoSelecionado = dispositivo;
                }
            }
        }
    }
}
