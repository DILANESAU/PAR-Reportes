using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class FamiliaLogicService
    {
        private readonly BusinessLogicService _businessLogic;

        public FamiliaLogicService(BusinessLogicService businessLogic)
        {
            _businessLogic = businessLogic;
        }

        // --------------------------------------------------------------------
        // 1. Resumen Global (Tarjetas Superiores)
        // --------------------------------------------------------------------
        public (List<FamiliaResumenModel> Arqui, List<FamiliaResumenModel> Espe) CalcularResumenGlobal(List<VentaReporteModel> ventas)
        {
            var arq = new List<FamiliaResumenModel>();
            var esp = new List<FamiliaResumenModel>();

            if ( ventas == null || !ventas.Any() ) return (arq, esp);

            var grupos = ventas.GroupBy(x => x.Familia).ToList();

            // Procesar Arquitectónica
            foreach ( var nombre in ConfiguracionLineas.Arquitectonica )
            {
                arq.Add(CrearTarjeta(nombre, grupos));
            }

            // Procesar Especializada
            foreach ( var nombre in ConfiguracionLineas.Especializada )
            {
                esp.Add(CrearTarjeta(nombre, grupos));
            }

            return (arq, esp);
        }

        private FamiliaResumenModel CrearTarjeta(string nombre, List<IGrouping<string, VentaReporteModel>> grupos)
        {
            var grupo = grupos.FirstOrDefault(g => g.Key == nombre);
            string color = _businessLogic.ObtenerColorFamilia(nombre);
            var modelo = new FamiliaResumenModel
            {
                NombreFamilia = nombre,
                ColorFondo = color,
                ColorTexto = ColorHelper.ObtenerColorTextto(color),
                VentaTotal = 0,
                LitrosTotales = 0,
                ProductoEstrella = "---"
            };

            if ( grupo != null )
            {
                modelo.VentaTotal = grupo.Sum(x => x.TotalVenta);
                modelo.LitrosTotales = grupo.Sum(x => x.LitrosTotales);

                var top = grupo.GroupBy(g => g.Descripcion)
                               .OrderByDescending(x => x.Sum(v => v.LitrosTotales))
                               .FirstOrDefault();
                modelo.ProductoEstrella = top?.Key ?? "---";
            }
            return modelo;
        }

        // --------------------------------------------------------------------
        // 2. ¡NUEVO MÉTODO FALTANTE! - Desglose por Periodos (Q1, Q2...)
        // --------------------------------------------------------------------
        // Nota: Aunque se llama "CalcularDesgloseClientes" por compatibilidad con el ViewModel,
        // aquí agrupa por SUB-LÍNEA (Linea) para mostrar el rendimiento de los productos.
        public List<SubLineaPerformanceModel> CalcularDesgloseClientes(List<VentaReporteModel> ventas, string periodo)
        {
            var resultado = new List<SubLineaPerformanceModel>();
            if ( ventas == null || !ventas.Any() ) return resultado;

            // Agrupamos por la propiedad LINEA (Ej: Impermeabilizantes -> Acrilicos, Asfalticos...)
            var grupos = ventas.GroupBy(x => x.Linea ?? "Otros");

            int mesActual = DateTime.Now.Month;

            foreach ( var grupo in grupos )
            {
                var item = new SubLineaPerformanceModel
                {
                    Nombre = grupo.Key,
                    VentaTotal = grupo.Sum(x => x.TotalVenta),
                    LitrosTotales = grupo.Sum(x => x.LitrosTotales),
                    Bloques = new List<PeriodoBloque>()
                };

                // Generar los bloques según el periodo solicitado
                if ( periodo == "SEMESTRAL" )
                {
                    item.Bloques.Add(CrearBloque("S1", grupo.ToList(), 1, 6, mesActual));
                    item.Bloques.Add(CrearBloque("S2", grupo.ToList(), 7, 12, mesActual));
                }
                else // TRIMESTRAL (Default)
                {
                    item.Bloques.Add(CrearBloque("Q1", grupo.ToList(), 1, 3, mesActual));
                    item.Bloques.Add(CrearBloque("Q2", grupo.ToList(), 4, 6, mesActual));
                    item.Bloques.Add(CrearBloque("Q3", grupo.ToList(), 7, 9, mesActual));
                    item.Bloques.Add(CrearBloque("Q4", grupo.ToList(), 10, 12, mesActual));
                }

                resultado.Add(item);
            }

            // Ordenamos por venta total descendente
            return resultado.OrderByDescending(x => x.VentaTotal).ToList();
        }

        private PeriodoBloque CrearBloque(string etiqueta, List<VentaReporteModel> ventas, int mesInicio, int mesFin, int mesActual)
        {
            // Filtramos las ventas que caen en este rango de meses
            var ventasPeriodo = ventas
                .Where(x => x.FechaEmision.Month >= mesInicio && x.FechaEmision.Month <= mesFin)
                .ToList();

            // Determinamos si es un periodo futuro para pintarlo gris en la UI
            // (Si el inicio del bloque es mayor al mes actual)
            bool esFuturo = mesInicio > mesActual;

            return new PeriodoBloque
            {
                Etiqueta = etiqueta,
                Valor = ventasPeriodo.Sum(x => x.TotalVenta),
                Litros = ventasPeriodo.Sum(x => x.LitrosTotales),
                EsFuturo = esFuturo
            };
        }

        // --------------------------------------------------------------------
        // 3. Utilidades (Ordenamiento, CSV, Vacíos)
        // --------------------------------------------------------------------
        public List<FamiliaResumenModel> OrdenarTarjetas(IEnumerable<FamiliaResumenModel> lista, string criterio)
        {
            if ( lista == null ) return new List<FamiliaResumenModel>();

            return criterio == "VENTA"
                ? lista.OrderByDescending(x => x.VentaTotal).ToList()
                : lista.OrderBy(x => x.NombreFamilia).ToList();
        }

        public string GenerarContenidoCSV(IEnumerable<VentaReporteModel> ventas)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fecha,Sucursal,Folio,Clave,Producto,Familia,Cliente,Cantidad,Litros Totales,Total Venta");

            foreach ( var v in ventas )
            {
                string desc = v.Descripcion?.Replace("\"", "\"\"") ?? "";
                string fam = v.Familia?.Replace("\"", "\"\"") ?? "";
                string cte = v.Cliente?.Replace("\"", "\"\"") ?? "";

                sb.AppendLine($"{v.FechaEmision:dd/MM/yyyy},{v.Sucursal},{v.MovID},{v.Articulo},\"{desc}\",\"{fam}\",\"{cte}\",{v.Cantidad},{v.LitrosTotales},{v.TotalVenta}");
            }
            return sb.ToString();
        }

        public List<FamiliaResumenModel> ObtenerTarjetasVacias(string lineaActual)
        {
            List<string> nombres;
            if ( lineaActual == "Arquitectonica" ) nombres = ConfiguracionLineas.Arquitectonica;
            else if ( lineaActual == "Especializada" ) nombres = ConfiguracionLineas.Especializada;
            else nombres = ConfiguracionLineas.ObtenerTodas();

            return nombres.Select(nombre =>
            {
                string color = _businessLogic.ObtenerColorFamilia(nombre);
                return new FamiliaResumenModel
                {
                    NombreFamilia = nombre,
                    ColorFondo = color,
                    ColorTexto = ColorHelper.ObtenerColorTextto(color),
                    VentaTotal = 0
                };
            }).ToList();
        }
    }
}