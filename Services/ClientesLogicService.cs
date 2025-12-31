using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class ClientesLogicService
    {
        public List<SubLineaPerformanceModel> CalcularDesgloseClientes(List<VentaReporteModel> datos, string modo)
        {
            if ( datos == null || !datos.Any() ) return new List<SubLineaPerformanceModel>();

            var resultado = new List<SubLineaPerformanceModel>();
            int mesActual = DateTime.Now.Month;
            string[] nombresMeses = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

            // 1. Agrupar por Cliente
            var grupos = datos
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Cliente) ? "CLIENTE SIN NOMBRE" : x.Cliente)
                .Select(g => new { Nombre = g.Key, Total = g.Sum(v => v.TotalVenta), Ventas = g.ToList() })
                .OrderByDescending(x => x.Total)
                .ToList();

            foreach ( var grupo in grupos )
            {
                var modelo = new SubLineaPerformanceModel
                {
                    Nombre = grupo.Nombre,
                    VentaTotal = grupo.Total,
                    Bloques = new List<PeriodoBloque>()
                };

                switch ( modo )
                {
                    case "MENSUAL": // 12 Barras (Ene - Dic)
                        for ( int m = 1; m <= 12; m++ )
                        {
                            modelo.Bloques.Add(new PeriodoBloque
                            {
                                Etiqueta = nombresMeses[m], // "Ene", "Feb"...
                                Valor = grupo.Ventas.Where(v => v.FechaEmision.Month == m).Sum(v => v.TotalVenta),
                                EsFuturo = m > mesActual
                            });
                        }
                        break;

                    case "TRIMESTRAL": // 4 Barras (Q1 - Q4)
                        for ( int i = 1; i <= 4; i++ )
                        {
                            int mesFin = i * 3;
                            int mesIni = mesFin - 2;
                            modelo.Bloques.Add(new PeriodoBloque
                            {
                                Etiqueta = $"Q{i}",
                                Valor = grupo.Ventas.Where(v => v.FechaEmision.Month >= mesIni && v.FechaEmision.Month <= mesFin).Sum(v => v.TotalVenta),
                                EsFuturo = mesIni > mesActual
                            });
                        }
                        break;

                    case "SEMESTRAL": // 2 Barras (S1 - S2)
                        modelo.Bloques.Add(new PeriodoBloque
                        {
                            Etiqueta = "S1",
                            Valor = grupo.Ventas.Where(v => v.FechaEmision.Month <= 6).Sum(v => v.TotalVenta),
                            EsFuturo = false
                        });
                        modelo.Bloques.Add(new PeriodoBloque
                        {
                            Etiqueta = "S2",
                            Valor = grupo.Ventas.Where(v => v.FechaEmision.Month > 6).Sum(v => v.TotalVenta),
                            EsFuturo = mesActual < 7
                        });
                        break;

                    case "ANUAL": // 1 Barra (Total Año)
                    default:
                        modelo.Bloques.Add(new PeriodoBloque
                        {
                            Etiqueta = "Total Año",
                            Valor = grupo.Total,
                            EsFuturo = false
                        });
                        break;
                }

                resultado.Add(modelo);
            }

            return resultado;
        }
    }
}
