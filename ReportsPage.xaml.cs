using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

// Aliases para evitar ambigüedad
using XamlPath = Microsoft.UI.Xaml.Shapes.Path;
using IOFile = System.IO.File;

namespace Formulario_1
{
    public class LeyendaItem
    {
        public string Etiqueta { get; set; } = "";
        public SolidColorBrush Color { get; set; } = new SolidColorBrush(Colors.Gray);
    }

    public sealed partial class ReportsPage : Page
    {
        private static readonly Color[] Paleta =
        {
            Color.FromArgb(255,   0, 121, 107),
            Color.FromArgb(255,  38, 166, 154),
            Color.FromArgb(255, 255, 167,  38),
            Color.FromArgb(255, 239,  83,  80),
            Color.FromArgb(255,  92, 107, 192),
            Color.FromArgb(255, 102, 187, 106),
            Color.FromArgb(255, 171,  71, 188),
            Color.FromArgb(255,  41, 182, 246),
        };

        // ─────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────
        public ReportsPage()
        {
            this.InitializeComponent();
            dpDesde.SelectedDate = null;
            dpHasta.SelectedDate = null;

            // Actualizar chip cuando cambian las fechas
            dpDesde.SelectedDateChanged += (s, e) => ActualizarChipFechas();
            dpHasta.SelectedDateChanged += (s, e) => ActualizarChipFechas();

            CargarDoctores();
        }

        // ─────────────────────────────────────────────────────────────────
        // Carga doctores desde usuarios.json (igual que FormularioPage)
        // ─────────────────────────────────────────────────────────────────
        private void CargarDoctores()
        {
            const string ruta = @"C:\AppCitas\usuarios.json";

            cbFiltroDoctor.Items.Clear();
            cbFiltroDoctor.Items.Add("Todos");

            if (!IOFile.Exists(ruta))
            {
                cbFiltroDoctor.SelectedIndex = 0;
                return;
            }

            try
            {
                var usuarios = JsonSerializer.Deserialize<List<UsuarioAcceso>>(IOFile.ReadAllText(ruta))
                               ?? new List<UsuarioAcceso>();

                foreach (var u in usuarios.Where(u => u.Rol == "Doctor"))
                    cbFiltroDoctor.Items.Add(u.Usuario);
            }
            catch { /* Si hay error de parseo, igual muestra "Todos" */ }

            cbFiltroDoctor.SelectedIndex = 0;
        }

        // ─────────────────────────────────────────────────────────────────
        // Chip de rango de fechas activo
        // ─────────────────────────────────────────────────────────────────
        private void ActualizarChipFechas()
        {
            DateTime? desde = dpDesde.SelectedDate?.DateTime.Date;
            DateTime? hasta = dpHasta.SelectedDate?.DateTime.Date;

            if (desde.HasValue || hasta.HasValue)
            {
                string texto = "";
                if (desde.HasValue && hasta.HasValue)
                    texto = $"📅 {desde.Value:dd/MM/yyyy}  →  {hasta.Value:dd/MM/yyyy}";
                else if (desde.HasValue)
                    texto = $"📅 Desde {desde.Value:dd/MM/yyyy}";
                else
                    texto = $"📅 Hasta {hasta.Value!:dd/MM/yyyy}";

                txtChipRango.Text = texto;
                chipRango.Visibility = Visibility.Visible;
            }
            else
            {
                chipRango.Visibility = Visibility.Collapsed;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Botón ✕ para limpiar fechas
        // ─────────────────────────────────────────────────────────────────
        private void LimpiarFechas_Click(object sender, RoutedEventArgs e)
        {
            dpDesde.SelectedDate = null;
            dpHasta.SelectedDate = null;
            chipRango.Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────────────────────────
        // Botón "Buscar"
        // ─────────────────────────────────────────────────────────────────
        private async void Buscar_Click(object sender, RoutedEventArgs e)
        {
            var todas = CargarTodasLasCitas();
            string texto = txtBuscar.Text.Trim();

            // --- Búsqueda por texto (ID o nombre) ---
            if (!string.IsNullOrWhiteSpace(texto))
            {
                List<Cita> res;
                if (int.TryParse(texto, out int id))
                {
                    var c = todas.FirstOrDefault(x => x.Id == id);
                    res = c != null ? new List<Cita> { c } : new List<Cita>();
                }
                else
                {
                    res = todas
                        .Where(x => x.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                MostrarResultados(res, $"Búsqueda: \"{texto}\"");
                return;
            }

            // --- Filtros combinados ---
            var q = todas.AsEnumerable();

            string? doctor = cbFiltroDoctor.SelectedItem?.ToString();
            bool filtroDoctor = !string.IsNullOrWhiteSpace(doctor) && doctor != "Todos";
            if (filtroDoctor)
                q = q.Where(x => x.Doctor == doctor);

            DateTime? desde = dpDesde.SelectedDate?.DateTime.Date;
            DateTime? hasta = dpHasta.SelectedDate?.DateTime.Date;

            if (desde.HasValue && hasta.HasValue && desde > hasta)
            {
                await MostrarMensaje("Error", "La fecha 'Desde' no puede ser mayor que 'Hasta'.");
                return;
            }

            if (desde.HasValue && hasta.HasValue)
                q = q.Where(x => x.FechaCita.Date >= desde.Value && x.FechaCita.Date <= hasta.Value);
            else if (desde.HasValue)
                q = q.Where(x => x.FechaCita.Date >= desde.Value);
            else if (hasta.HasValue)
                q = q.Where(x => x.FechaCita.Date <= hasta.Value);

            // Construir descripción del contexto para el chip
            string contexto = filtroDoctor ? doctor! : "Todos los doctores";
            if (desde.HasValue && hasta.HasValue)
                contexto += $"  |  {desde.Value:dd/MM/yy} – {hasta.Value:dd/MM/yy}";
            else if (desde.HasValue)
                contexto += $"  |  desde {desde.Value:dd/MM/yy}";
            else if (hasta.HasValue)
                contexto += $"  |  hasta {hasta.Value:dd/MM/yy}";

            MostrarResultados(q.ToList(), contexto);
        }

        // ─────────────────────────────────────────────────────────────────
        // Muestra resultados + actualiza chip + gráficas
        // ─────────────────────────────────────────────────────────────────
        private void MostrarResultados(List<Cita> citas, string contexto)
        {
            lvResultados.ItemsSource = citas;

            // Contador
            int n = citas.Count;
            txtContador.Text = n == 1 ? "1 cita" : $"{n} citas";

            // Chip de contexto de estadísticas
            if (!string.IsNullOrWhiteSpace(contexto))
            {
                txtChipContexto.Text = contexto;
                chipContexto.Visibility = Visibility.Visible;
            }
            else
            {
                chipContexto.Visibility = Visibility.Collapsed;
            }

            DibujarGraficas(citas);
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────
        private List<Cita> CargarTodasLasCitas()
        {
            const string ruta = @"C:\AppCitas\citas.json";
            if (!IOFile.Exists(ruta)) return new List<Cita>();

            try
            {
                return JsonSerializer.Deserialize<List<Cita>>(IOFile.ReadAllText(ruta))
                       ?? new List<Cita>();
            }
            catch { return new List<Cita>(); }
        }

        private async Task MostrarMensaje(string titulo, string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // Orquesta las 4 gráficas
        // ─────────────────────────────────────────────────────────────────
        private void DibujarGraficas(List<Cita> citas)
        {
            DibujarDona(citas);
            DibujarBarrasDias(citas);
            DibujarBarrasEdad(citas);
            DibujarLineaMeses(citas);
        }

        // ═══════════════════════════════════════════════════════════════════
        // GRÁFICA 1 — Dona: citas por doctor
        // ═══════════════════════════════════════════════════════════════════
        private void DibujarDona(List<Cita> citas)
        {
            canvasDoctores.Children.Clear();
            leyendaDoctores.ItemsSource = null;

            if (!citas.Any())
            {
                AgregarTextoVacio(canvasDoctores, 110, 110);
                return;
            }

            var grupos = citas
                .GroupBy(c => c.Doctor)
                .Select(g => new { Doctor = g.Key, Total = g.Count() })
                .OrderByDescending(g => g.Total)
                .ToList();

            double total = grupos.Sum(g => g.Total);
            double cx = 110, cy = 110, rOut = 90, rIn = 50;
            double ang = -Math.PI / 2;
            var leyendas = new List<LeyendaItem>();

            for (int i = 0; i < grupos.Count; i++)
            {
                double frac = grupos[i].Total / total;
                double delta = frac * 2 * Math.PI;
                Color color = Paleta[i % Paleta.Length];
                double angFin = ang + delta;
                bool grande = delta > Math.PI;

                Point p1o = new Point(cx + rOut * Math.Cos(ang), cy + rOut * Math.Sin(ang));
                Point p2o = new Point(cx + rOut * Math.Cos(angFin), cy + rOut * Math.Sin(angFin));
                Point p1i = new Point(cx + rIn * Math.Cos(ang), cy + rIn * Math.Sin(ang));
                Point p2i = new Point(cx + rIn * Math.Cos(angFin), cy + rIn * Math.Sin(angFin));

                var fig = new PathFigure { StartPoint = p1o, IsClosed = true };
                fig.Segments.Add(new ArcSegment { Point = p2o, Size = new Size(rOut, rOut), IsLargeArc = grande, SweepDirection = SweepDirection.Clockwise });
                fig.Segments.Add(new LineSegment { Point = p2i });
                fig.Segments.Add(new ArcSegment { Point = p1i, Size = new Size(rIn, rIn), IsLargeArc = grande, SweepDirection = SweepDirection.Counterclockwise });

                var geom = new PathGeometry();
                geom.Figures.Add(fig);

                canvasDoctores.Children.Add(new XamlPath
                {
                    Data = geom,
                    Fill = new SolidColorBrush(color),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                });

                // % en el segmento
                double angMid = ang + delta / 2;
                double rLbl = (rOut + rIn) / 2;
                var tbPct = new TextBlock
                {
                    Text = $"{frac:P0}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                Canvas.SetLeft(tbPct, cx + rLbl * Math.Cos(angMid) - 14);
                Canvas.SetTop(tbPct, cy + rLbl * Math.Sin(angMid) - 8);
                canvasDoctores.Children.Add(tbPct);

                leyendas.Add(new LeyendaItem
                {
                    Etiqueta = $"{grupos[i].Doctor} ({grupos[i].Total})",
                    Color = new SolidColorBrush(color)
                });

                ang = angFin;
            }

            // Texto central
            var tbCentro = new TextBlock
            {
                Text = $"{(int)total}\nCitas",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 121, 107)),
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(tbCentro, cx - 22);
            Canvas.SetTop(tbCentro, cy - 16);
            canvasDoctores.Children.Add(tbCentro);

            leyendaDoctores.ItemsSource = leyendas;
        }

        // ═══════════════════════════════════════════════════════════════════
        // GRÁFICA 2 — Barras horizontales: días de la semana
        // ═══════════════════════════════════════════════════════════════════
        private void DibujarBarrasDias(List<Cita> citas)
        {
            canvasDias.Children.Clear();

            if (!citas.Any()) { AgregarTextoVacio(canvasDias, 200, 110); return; }

            string[] nombres = { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };
            int[] conteo = new int[7];
            foreach (var c in citas)
                conteo[(int)c.FechaCita.DayOfWeek]++;

            int maxVal = conteo.Max();
            if (maxVal == 0) return;

            double mL = 45, mD = 30, mT = 8;
            double W = 460, H = 220;
            double barH = (H - mT - 6 * 8) / 7;

            for (int i = 0; i < 7; i++)
            {
                double y = mT + i * (barH + 8);
                double barW = (W - mL - mD) * conteo[i] / maxVal;

                // Fondo
                canvasDias.Children.Add(RectCanvas(W - mL - mD, barH, Color.FromArgb(255, 235, 235, 235), mL, y, 4));

                // Barra
                if (barW > 0)
                    canvasDias.Children.Add(RectCanvas(barW, barH, Paleta[i % Paleta.Length], mL, y, 4));

                // Etiqueta día
                AgregarTextCanvas(canvasDias, nombres[i], 11, Colors.Black, false, 2, y + barH / 2 - 8);

                // Valor
                AgregarTextCanvas(canvasDias, conteo[i].ToString(), 11, Colors.Black, true, mL + barW + 4, y + barH / 2 - 8);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // GRÁFICA 3 — Barras verticales: distribución de edades
        // ═══════════════════════════════════════════════════════════════════
        private void DibujarBarrasEdad(List<Cita> citas)
        {
            canvasEdades.Children.Clear();

            if (!citas.Any()) { AgregarTextoVacio(canvasEdades, 200, 110); return; }

            string[] rangos = { "0-9", "10-19", "20-29", "30-39", "40-49", "50-59", "60-69", "70+" };
            int[] conteo = new int[rangos.Length];
            foreach (var c in citas)
                conteo[Math.Min(c.Edad / 10, rangos.Length - 1)]++;

            int maxVal = conteo.Max();
            if (maxVal == 0) return;

            double mL = 30, mD = 10, mT = 20, mB = 30;
            double W = 460, H = 220;
            double dispW = W - mL - mD;
            double barW = dispW / rangos.Length - 6;

            // Cuadrícula
            for (int g = 1; g <= 4; g++)
            {
                double yG = mT + (H - mT - mB) * (1 - g / 4.0);
                canvasEdades.Children.Add(new Line
                {
                    X1 = mL,
                    Y1 = yG,
                    X2 = W - mD,
                    Y2 = yG,
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)),
                    StrokeThickness = 1
                });
                AgregarTextCanvas(canvasEdades, $"{(int)(maxVal * g / 4.0)}", 9, Colors.Gray, false, 0, yG - 8);
            }

            for (int i = 0; i < rangos.Length; i++)
            {
                double x = mL + i * (dispW / rangos.Length) + 3;
                double alto = (H - mT - mB) * conteo[i] / maxVal;
                double y = H - mB - alto;

                canvasEdades.Children.Add(RectCanvas(barW, Math.Max(alto, 2), Paleta[(i + 2) % Paleta.Length], x, y, 4));

                if (conteo[i] > 0)
                    AgregarTextCanvas(canvasEdades, conteo[i].ToString(), 10, Colors.Black, true, x + barW / 2 - 6, y - 16);

                AgregarTextCanvas(canvasEdades, rangos[i], 9, Colors.Black, false, x + barW / 2 - 10, H - mB + 4);
            }

            // Línea base
            canvasEdades.Children.Add(new Line
            {
                X1 = mL,
                Y1 = H - mB,
                X2 = W - mD,
                Y2 = H - mB,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)),
                StrokeThickness = 1
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // GRÁFICA 4 — Línea con área: citas por mes
        // ═══════════════════════════════════════════════════════════════════
        private void DibujarLineaMeses(List<Cita> citas)
        {
            canvasMeses.Children.Clear();

            if (!citas.Any()) { AgregarTextoVacio(canvasMeses, 200, 110); return; }

            var grupos = citas
                .GroupBy(c => new { c.FechaCita.Year, c.FechaCita.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new { Label = $"{g.Key.Month:D2}/{g.Key.Year % 100:D2}", Total = g.Count() })
                .ToList();

            int maxVal = grupos.Max(g => g.Total);
            if (maxVal == 0) return;

            double mL = 35, mD = 15, mT = 20, mB = 35;
            double W = 460, H = 220;
            double dispW = W - mL - mD, dispH = H - mT - mB;

            double GetX(int i) => mL + (grupos.Count == 1 ? dispW / 2 : i * dispW / (grupos.Count - 1));
            double GetY(int i) => mT + dispH * (1 - grupos[i].Total / (double)maxVal);

            // Cuadrícula
            for (int g = 0; g <= 4; g++)
            {
                double yG = mT + dispH * (1 - g / 4.0);
                canvasMeses.Children.Add(new Line
                {
                    X1 = mL,
                    Y1 = yG,
                    X2 = W - mD,
                    Y2 = yG,
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                });
                AgregarTextCanvas(canvasMeses, $"{(int)(maxVal * g / 4.0)}", 9, Colors.Gray, false, 0, yG - 8);
            }

            // Área bajo la curva
            var areaFig = new PathFigure { IsClosed = true };
            areaFig.StartPoint = new Point(GetX(0), H - mB);
            for (int i = 0; i < grupos.Count; i++)
                areaFig.Segments.Add(new LineSegment { Point = new Point(GetX(i), GetY(i)) });
            areaFig.Segments.Add(new LineSegment { Point = new Point(GetX(grupos.Count - 1), H - mB) });

            var areaGeom = new PathGeometry();
            areaGeom.Figures.Add(areaFig);
            canvasMeses.Children.Add(new XamlPath
            {
                Data = areaGeom,
                Fill = new SolidColorBrush(Color.FromArgb(40, 0, 121, 107))
            });

            // Líneas entre puntos
            for (int i = 0; i < grupos.Count - 1; i++)
            {
                canvasMeses.Children.Add(new Line
                {
                    X1 = GetX(i),
                    Y1 = GetY(i),
                    X2 = GetX(i + 1),
                    Y2 = GetY(i + 1),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 121, 107)),
                    StrokeThickness = 2.5
                });
            }

            // Puntos y etiquetas
            for (int i = 0; i < grupos.Count; i++)
            {
                double xi = GetX(i), yi = GetY(i);

                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Colors.White),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 121, 107)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(dot, xi - 4); Canvas.SetTop(dot, yi - 4);
                canvasMeses.Children.Add(dot);

                AgregarTextCanvas(canvasMeses, grupos[i].Label, 9, Colors.Black, false, xi - 14, H - mB + 4);

                if (grupos.Count <= 14)
                    AgregarTextCanvas(canvasMeses, grupos[i].Total.ToString(), 10,
                        Color.FromArgb(255, 0, 121, 107), true, xi - 6, yi - 20);
            }

            // Línea base
            canvasMeses.Children.Add(new Line
            {
                X1 = mL,
                Y1 = H - mB,
                X2 = W - mD,
                Y2 = H - mB,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)),
                StrokeThickness = 1
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers de dibujo
        // ─────────────────────────────────────────────────────────────────
        private static Rectangle RectCanvas(double w, double h, Color color, double left, double top, double radius = 0)
        {
            var r = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = new SolidColorBrush(color),
                RadiusX = radius,
                RadiusY = radius
            };
            Canvas.SetLeft(r, left);
            Canvas.SetTop(r, top);
            return r;
        }

        private static void AgregarTextCanvas(Canvas canvas, string texto, double fontSize,
            Color color, bool bold, double left, double top)
        {
            var tb = new TextBlock
            {
                Text = texto,
                FontSize = fontSize,
                Foreground = new SolidColorBrush(color),
                FontWeight = bold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal
            };
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, top);
            canvas.Children.Add(tb);
        }

        private static void AgregarTextoVacio(Canvas canvas, double left, double top)
        {
            AgregarTextCanvas(canvas, "Sin datos para mostrar", 12, Colors.Gray, false, left - 60, top);
        }
    }
}
