using System;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothTeste.Contrato
{
    public interface IServicoBluetooth
    {
        Action<string> AcaoMensagem { get; set; }

        Task ExecutarAsync(int tempoMaximoExecucao, int quantidadeMaximaTentativas, CancellationTokenSource tokenCancelamento);
    }
}
