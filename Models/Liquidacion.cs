namespace Nomina.WorkerTimbrado.Models
{
    public class Liquidacion
    {
        public int IdLiquidacion { get; set; }
        public int IdCompania { get; set; }
        public int Intentos { get; set; }
        public short UltimoIntento { get; set; }
    }
}
