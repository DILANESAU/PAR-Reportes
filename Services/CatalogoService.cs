using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class CatalogoService
    {
        private Dictionary<string, ProductoInfo> _catalogo;
        public CatalogoService()
        {
            _catalogo = [];
            CargarDesdeCSV();
        }
        private void CargarDesdeCSV()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Productos.csv");

                if ( !File.Exists(path) ) return;

                var lineas = File.ReadAllLines(path).Skip(1);

                foreach ( var linea in lineas )
                {
                    var col = ParseCsvLine(linea);
                    if ( col.Count < 8 ) continue;
                    string clave = col[0].Trim();
                    string descripcion = col[1].Trim().Replace("\"", "");
                    string familiaRaw = col[4].Trim();
                    string litrosStr = col[7].Trim();
                    string lineaRaw = col.Count > 5 ? col[5].Trim() : "Sin Linea";
                    string colorRaw = col.Count > 6 ? col[6].Trim() : "Sin Color";

                    if ( !double.TryParse(litrosStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double litros) )
                        litros = 0;
                    if ( !_catalogo.ContainsKey(clave) )
                    {
                        _catalogo.Add(clave, new ProductoInfo
                        {
                            Clave = clave,
                            Descripcion = descripcion,
                            FamiliaSimple = NormalizarFamilia(familiaRaw),
                            Litros = litros,
                            Linea = lineaRaw,
                            Color = colorRaw,
                        });
                    }
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine("Error cargando CSV: " + ex.Message);
                throw;
            }
        }
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for ( int i = 0; i < line.Length; i++ )
            {
                char c = line[i];

                if ( c == '"' )
                {
                    inQuotes = !inQuotes;
                }
                else if ( c == ',' && !inQuotes )
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            result.Add(currentField);
            return result;
        }
        private string NormalizarFamilia(string raw)
        {
            if ( string.IsNullOrEmpty(raw) ) return "Otros";

            string mayus = raw.ToUpper().Trim();

            if ( mayus.Contains("SELLADOR") ) return "Selladores";
            if ( mayus.Contains("IMPER") ) return "Impermeabilizantes";
            if ( mayus.Contains("TRAFICO") ) return "Tráfico";
            if ( mayus.Contains("INDUSTRIAL") ) return "Industrial";
            if ( mayus.Contains("VINIL") ) return "Vinílica";
            if ( mayus.Contains("ESMALTE") ) return "Esmaltes";

            if ( mayus.Contains("MADERAS") ) return "Maderas";
            if ( mayus.Contains("SOLVENTES") ) return "Solventes";
            if ( mayus.Contains("FER-") ) return "Ferretería";
            if ( mayus.Contains("ACCESORIOS") ) return "Accesorios";
            if ( mayus.Contains("AF-") ) return "Activos Fijos";

            return "Otros";
        }
        public ProductoInfo ObtenerInfo(string claveProducto)
        {
            if ( _catalogo.ContainsKey(claveProducto) ) return _catalogo[claveProducto];

            return new ProductoInfo { FamiliaSimple = "Accesorios", Litros = 0 };
        }
    }
}
