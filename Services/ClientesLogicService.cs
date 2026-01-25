using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class ClientesLogicService
    {
        public List<ClienteResumenModel> ProcesarClientes(List<VentaReporteModel> ventasActuales, List<VentaReporteModel> ventasAnteriores)
        {
            // 1. Obtener lista única de clientes (de ambos años para no perder a los que dejaron de comprar)
            var todosLosClientes = ventasActuales.Select(x => x.Cliente)
                .Union(ventasAnteriores.Select(x => x.Cliente))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var resultado = new List<ClienteResumenModel>();

            foreach ( var cliente in todosLosClientes )
            {

                var row = new ClienteResumenModel { Nombre = cliente };


                // --- DATOS AÑO ACTUAL ---
                var vActual = ventasActuales.Where(x => x.Cliente == cliente).ToList();

                for ( int i = 1; i <= 12; i++ )
                {
                    row.HistoriaMensualActual.Add(vActual.Where(x => x.FechaEmision.Month == i).Sum(v => v.TotalVenta));
                }

                row.VentaAnualActual = vActual.Sum(x => x.TotalVenta);
                row.LitrosAnualActual = vActual.Sum(x => x.LitrosTotales);

                // Trimestres Actuales
                row.VentaQ1Actual = vActual.Where(x => x.FechaEmision.Month <= 3).Sum(x => x.TotalVenta);
                row.LitrosQ1Actual = vActual.Where(x => x.FechaEmision.Month <= 3).Sum(x => x.LitrosTotales);

                row.VentaQ2Actual = vActual.Where(x => x.FechaEmision.Month > 3 && x.FechaEmision.Month <= 6).Sum(x => x.TotalVenta);
                row.LitrosQ2Actual = vActual.Where(x => x.FechaEmision.Month > 3 && x.FechaEmision.Month <= 6).Sum(x => x.LitrosTotales);
                // ... (Igual para Q3 y Q4)

                // Semestres Actuales
                row.VentaS1Actual = row.VentaQ1Actual + row.VentaQ2Actual;
                row.LitrosS1Actual = row.LitrosQ1Actual + row.LitrosQ2Actual;


                // --- DATOS AÑO ANTERIOR ---
                var vAnterior = ventasAnteriores.Where(x => x.Cliente == cliente).ToList();
                row.VentaAnualAnterior = vAnterior.Sum(x => x.TotalVenta);

                // Trimestres Anteriores (Para comparar)
                row.VentaQ1Anterior = vAnterior.Where(x => x.FechaEmision.Month <= 3).Sum(x => x.TotalVenta);
                row.VentaQ2Anterior = vAnterior.Where(x => x.FechaEmision.Month > 3 && x.FechaEmision.Month <= 6).Sum(x => x.TotalVenta);

                // Semestres Anteriores
                row.VentaS1Anterior = row.VentaQ1Anterior + row.VentaQ2Anterior;

                resultado.Add(row);
            }

            // Ordenar por venta actual descendente (Pareto natural)
            return resultado.OrderByDescending(x => x.VentaAnualActual).ToList();
        }
    }
}
