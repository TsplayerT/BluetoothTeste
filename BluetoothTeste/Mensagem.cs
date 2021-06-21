namespace BluetoothTeste
{
    public class Mensagem
    {
        public string Hora { get; set; }
        public string Texto { get; set; }

        public Mensagem(string hora, string texto)
        {
            Hora = hora;
            Texto = texto;
        }
    }
}
