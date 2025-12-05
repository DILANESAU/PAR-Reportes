using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WPF_PAR.Services
{
    public class SucursalesService
    {
        public Dictionary<int, string> CargarSucursales()
        {
            var diccionario = new Dictionary<int, string>();

            try
            {
                // Buscamos el archivo en la carpeta Assets (igual que Productos.csv)
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sucursales.csv");

                if ( !File.Exists(path) )
                {
                    // Si no existe, devolvemos al menos la Matriz para que no truene
                    diccionario.Add(1508, "Sucursal Default (Archivo no encontrado)");
                    return diccionario;
                }

                // Leemos todas las líneas
                var lineas = File.ReadAllLines(path);

                // Saltamos la primera línea (Encabezados: Sucursal,Nombre)
                foreach ( var linea in lineas.Skip(1) )
                {
                    // Separamos por comas
                    var columnas = linea.Split(',');

                    if ( columnas.Length >= 2 )
                    {
                        // Columna 0: ID (Ej: 1508)
                        string idString = columnas[0].Trim();

                        // Columna 1 en adelante: Nombre (Ej: Ocosingo - C Azul Pacifico)
                        // Usamos Join por si el nombre contiene comas intermedias
                        string nombre = string.Join(",", columnas.Skip(1)).Trim();

                        if ( int.TryParse(idString, out int id) )
                        {
                            if ( !diccionario.ContainsKey(id) )
                            {
                                diccionario.Add(id, nombre);
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine("Error cargando sucursales: " + ex.Message);
            }

            // Ordenar por ID para que salgan bonitas en la lista
            return diccionario.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
