using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothTeste
{
    public static class Utilidade
    {
        public static Dictionary<string, string> ListaUuid => new Dictionary<string, string>
        {
            { "ServiceDiscoveryServerServiceClassID", "00001000-0000-1000-8000-00805F9B34FB" },
            { "BrowseGroupDescriptorServiceClassID", "00001001-0000-1000-8000-00805F9B34FB" },
            { "PublicBrowseGroupServiceClass", "00001002-0000-1000-8000-00805F9B34FB" },
            { "SerialPortServiceClass", "00001101-0000-1000-8000-00805F9B34FB" },
            { "LANAccessUsingPPPServiceClass", "00001102-0000-1000-8000-00805F9B34FB" },
            { "DialupNetworkingServiceClas", "00001103-0000-1000-8000-00805F9B34FB" },
            { "IrMCSyncServiceClass", "00001104-0000-1000-8000-00805F9B34FB" },
            { "OBEXObjectPushServiceClass", "00001105-0000-1000-8000-00805F9B34FB" },
            { "OBEXFileTransferServiceClass", "00001106-0000-1000-8000-00805F9B34FB" },
            { "IrMCSyncCommandServiceClass", "00001107-0000-1000-8000-00805F9B34FB" },
            { "HeadsetServiceClass", "00001108-0000-1000-8000-00805F9B34FB" },
            { "CordlessTelephonyServiceClass", "00001109-0000-1000-8000-00805F9B34FB" },
            { "AudioSourceServiceClass", "0000110A-0000-1000-8000-00805F9B34FB" },
            { "AudioSinkServiceClass", "0000110B-0000-1000-8000-00805F9B34FB" },
            { "AVRemoteControlTargetServiceClass", "0000110C-0000-1000-8000-00805F9B34FB" },
            { "AdvancedAudioDistributionServiceClass", "0000110D-0000-1000-8000-00805F9B34FB" },
            { "AVRemoteControlServiceClass", "0000110E-0000-1000-8000-00805F9B34FB" },
            { "VideoConferencingServiceClass", "0000110F-0000-1000-8000-00805F9B34FB" },
            { "IntercomServiceClass", "00001110-0000-1000-8000-00805F9B34FB" },
            { "FaxServiceClass", "00001111-0000-1000-8000-00805F9B34FB" },
            { "HeadsetAudioGatewayServiceClass", "00001112-0000-1000-8000-00805F9B34FB" },
            { "WAPServiceClass", "00001113-0000-1000-8000-00805F9B34FB" },
            { "WAPClientServiceClass", "00001114-0000-1000-8000-00805F9B34FB" },
            { "PANUServiceClass", "00001115-0000-1000-8000-00805F9B34FB" },
            { "NAPServiceClass", "00001116-0000-1000-8000-00805F9B34FB" },
            { "GNServiceClass", "00001117-0000-1000-8000-00805F9B34FB" },
            { "DirectPrintingServiceClass", "00001118-0000-1000-8000-00805F9B34FB" },
            { "ReferencePrintingServiceClass", "00001119-0000-1000-8000-00805F9B34FB" },
            { "ImagingServiceClass", "0000111A-0000-1000-8000-00805F9B34FB" },
            { "ImagingResponderServiceClass", "0000111B-0000-1000-8000-00805F9B34FB" },
            { "ImagingAutomaticArchiveServiceClass", "0000111C-0000-1000-8000-00805F9B34FB" },
            { "ImagingReferenceObjectsServiceClass", "0000111D-0000-1000-8000-00805F9B34FB" },
            { "HandsfreeServiceClass", "0000111E-0000-1000-8000-00805F9B34FB" },
            { "HandsfreeAudioGatewayServiceClass", "0000111F-0000-1000-8000-00805F9B34FB" },
            { "DirectPrintingReferenceObjectsServiceClass", "00001120-0000-1000-8000-00805F9B34FB" },
            { "ReflectedUIServiceClass", "00001121-0000-1000-8000-00805F9B34FB" },
            { "BasicPringingServiceClass", "00001122-0000-1000-8000-00805F9B34FB" },
            { "PrintingStatusServiceClass", "00001123-0000-1000-8000-00805F9B34FB" },
            { "HumanInterfaceDeviceServiceClass", "00001124-0000-1000-8000-00805F9B34FB" },
            { "HardcopyCableReplacementServiceClass", "00001125-0000-1000-8000-00805F9B34FB" },
            { "HCRPrintServiceClas", "00001126-0000-1000-8000-00805F9B34FB" },
            { "HCRScanServiceClass", "00001127-0000-1000-8000-00805F9B34FB" },
            { "CommonISDNAccessServiceClass", "00001128-0000-1000-8000-00805F9B34FB" },
            { "VideoConferencingGWServiceClass", "00001129-0000-1000-8000-00805F9B34FB" },
            { "UDIMTServiceClass", "0000112A-0000-1000-8000-00805F9B34FB" },
            { "UDITAServiceClass", "0000112B-0000-1000-8000-00805F9B34FB" },
            { "AudioVideoServiceClass", "0000112C-0000-1000-8000-00805F9B34FB" },
            { "SIMAccessServiceClass", "0000112D-0000-1000-8000-00805F9B34FB" },
            { "PnPInformationServiceClass", "00001200-0000-1000-8000-00805F9B34FB" },
            { "GenericNetworkingServiceClass", "00001201-0000-1000-8000-00805F9B34FB" },
            { "GenericFileTransferServiceClass", "00001202-0000-1000-8000-00805F9B34FB" },
            { "GenericAudioServiceClass", "00001203-0000-1000-8000-00805F9B34FB" },
            { "GenericTelephonyServiceClass", "00001204-0000-1000-8000-00805F9B34FB" }
        };

        public static void ExecucaoSegura(this Action<string> acaoMensagem, string nomeMetodo, Func<bool?> acaoMetodo)
        {
            if (acaoMetodo != null)
            {
                try
                {
                    var valor = acaoMetodo.Invoke();
                    var valorTratado = valor != null ? Convert.ToString(valor) : "null";

                    acaoMensagem?.Invoke($"[{nomeMetodo}] {valorTratado}");
                }
                catch (Exception ex)
                {
                    acaoMensagem?.Invoke($"[{nomeMetodo}] {ex.Message}");
                }
            }
        }

        public static async Task ParallelForEachAsync<T>(IEnumerable<T> source, Func<T, Task> funcBody, int maxDoP, Action<string> acaoMensagem, CancellationToken token)
        {
            async Task AwaitPartition(IEnumerator<T> partition)
            {
                using (partition)
                {
                    while (partition.MoveNext())
                    {
                        //evita que a sincronização / hot thread desligue
                        await Task.Yield();
                        await funcBody(partition.Current).ConfigureAwait(false);
                    }
                }
            }

            try
            {
                acaoMensagem?.Invoke($"[{nameof(ParallelForEachAsync)}] iniciando");

                var tasks = Partitioner.Create(source).GetPartitions(maxDoP).AsParallel().WithCancellation(token).WithDegreeOfParallelism(maxDoP).WithExecutionMode(ParallelExecutionMode.ForceParallelism).Select(async x => await AwaitPartition(x).ConfigureAwait(true)).ToList();

                await Task.WhenAll(tasks).ConfigureAwait(false);

                acaoMensagem?.Invoke($"[{nameof(ParallelForEachAsync)}] concluido");
            }
            catch (Exception ex)
            {
                acaoMensagem?.Invoke($"[{nameof(ParallelForEachAsync)}] {ex.Message}");
            }
        }
    }
}
