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

        // 1. Lógica para generar las tarjetas de resumen (Arquitectónica/Especializada)
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

        // 2. Lógica de Ordenamiento
        public List<FamiliaResumenModel> OrdenarTarjetas(IEnumerable<FamiliaResumenModel> lista, string criterio)
        {
            if ( lista == null ) return new List<FamiliaResumenModel>();

            return criterio == "VENTA"
                ? lista.OrderByDescending(x => x.VentaTotal).ToList()
                : lista.OrderBy(x => x.NombreFamilia).ToList();
        }

        // 3. Lógica para preparar el CSV (Sin guardar archivo, solo generar el texto)
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

        // 4. Lógica para inicializar vacío
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
        // En Services/FamiliaLogicService.cs

        public List<SubLineaPerformanceModel> CalcularDesgloseClientes(List<VentaReporteModel> datos, string modo)
        {
            if ( datos == null || !datos.Any() ) return new List<SubLineaPerformanceModel>();

            var resultado = new List<SubLineaPerformanceModel>();
            int mesActual = DateTime.Now.Month;

            // 1. Agrupar (Aquí tomamos TODOS los clientes del año)
            var grupos = datos
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Cliente) ? "CLIENTE SIN NOMBRE" : x.Cliente)
                .Select(g => new {
                    Nombre = g.Key,
                    TotalDinero = g.Sum(v => v.TotalVenta),
                    TotalLitros = g.Sum(v => v.LitrosTotales), // Suma anual litros
                    Ventas = g.ToList()
                })
                .OrderByDescending(x => x.TotalDinero)
                .ToList();

            foreach ( var grupo in grupos )
            {
                var modelo = new SubLineaPerformanceModel
                {
                    Nombre = grupo.Nombre,
                    VentaTotal = grupo.TotalDinero,
                    LitrosTotales = grupo.TotalLitros, // Guardamos el total anual
                    Bloques = new List<PeriodoBloque>()
                };

                // Lógica de Bloques (Ahora sumamos Litros también)
                if ( modo == "TRIMESTRAL" )
                {
                    for ( int i = 1; i <= 4; i++ )
                    {
                        int mesFin = i * 3;
                        int mesIni = mesFin - 2;
                        bool esFuturo = mesIni > mesActual;

                        // Filtramos las ventas de este periodo
                        var ventasPeriodo = grupo.Ventas
                            .Where(v => v.FechaEmision.Month >= mesIni && v.FechaEmision.Month <= mesFin)
                            .ToList();

                        modelo.Bloques.Add(new PeriodoBloque
                        {
                            Etiqueta = $"Q{i}",
                            Valor = ventasPeriodo.Sum(v => v.TotalVenta), // Si no hay ventas, Sum da 0 (Correcto)
                            Litros = ventasPeriodo.Sum(v => v.LitrosTotales), // <--- Suma de Litros
                            EsFuturo = esFuturo
                        });
                    }
                }
                else if ( modo == "MENSUAL" )
                {
                    string[] meses = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
                    for ( int m = 1; m <= 12; m++ )
                    {
                        var ventasMes = grupo.Ventas.Where(v => v.FechaEmision.Month == m).ToList();
                        modelo.Bloques.Add(new PeriodoBloque
                        {
                            Etiqueta = meses[m],
                            Valor = ventasMes.Sum(v => v.TotalVenta),
                            Litros = ventasMes.Sum(v => v.LitrosTotales), // <--- Suma Litros
                            EsFuturo = m > mesActual
                        });
                    }
                }
                // ... (Igual para Semestral/Anual) ...

                resultado.Add(modelo);
            }

            return resultado;
        }
    }
}