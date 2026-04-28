using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.RegularExpressions;

namespace Formulario_1
{
    public sealed partial class FormularioPage : Page
    {
        // --- CONSTRUCTOR ---
        // Inicializa componentes, tema y carga datos iniciales
        public FormularioPage()
        {
            this.InitializeComponent();
            this.RequestedTheme = ElementTheme.Light;

            dpNacimiento.MinDate = new DateTimeOffset(new DateTime(1910, 1, 1));

            CargarDoctores();
        }

        // --- EVENTO DE NAVEGACIÓN ---
        // Se ejecuta cada vez que se entra a la página
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            CargarDoctores(); // refresca lista de doctores
        }

        // =====================================================
        //                   CARGA DE DATOS
        // =====================================================

        // --- CARGAR DOCTORES ---
        // Obtiene los usuarios con rol "Doctor" y los agrega al ComboBox
        private void CargarDoctores()
        {
            var usuarios = CargarUsuarios();

            cbDoctores.Items.Clear();

            cbDoctores.Items.Add(new ComboBoxItem
            {
                Content = "-- Selecciona un doctor --",
                IsEnabled = false
            });

            foreach (var u in usuarios.Where(u => u.Rol == "Doctor"))
            {
                cbDoctores.Items.Add(new ComboBoxItem
                {
                    Content = u.Usuario
                });
            }

            cbDoctores.SelectedIndex = 0;
        }

        // --- CARGAR USUARIOS DESDE JSON ---
        private List<UsuarioAcceso> CargarUsuarios()
        {
            string ruta = @"C:\AppCitas\usuarios.json";

            if (!File.Exists(ruta))
                return new List<UsuarioAcceso>();

            string json = File.ReadAllText(ruta);

            return JsonSerializer.Deserialize<List<UsuarioAcceso>>(json)
                   ?? new List<UsuarioAcceso>();
        }

        // =====================================================
        //              EVENTOS DE UI (CHECKBOXES)
        // =====================================================

        // --- ENFERMEDADES ---
        private void chkEnfSi_Checked(object sender, RoutedEventArgs e)
        {
            txtenfermedades.Visibility = Visibility.Visible;
            chkEnfNo.IsChecked = false;
        }

        private void chkEnfSi_Unchecked(object sender, RoutedEventArgs e)
        {
            txtenfermedades.Visibility = Visibility.Collapsed;
        }

        private void chkEnfNo_Checked(object sender, RoutedEventArgs e)
        {
            txtenfermedades.Visibility = Visibility.Collapsed;
            txtenfermedades.Text = "";
            chkEnfSi.IsChecked = false;
        }

        // --- CIRUGÍAS ---
        private void chkCirSi_Checked(object sender, RoutedEventArgs e)
        {
            txtcirugias.Visibility = Visibility.Visible;
            chkCirNo.IsChecked = false;
        }

        private void chkCirSi_Unchecked(object sender, RoutedEventArgs e)
        {
            txtcirugias.Visibility = Visibility.Collapsed;
        }

        private void chkCirNo_Checked(object sender, RoutedEventArgs e)
        {
            txtcirugias.Visibility = Visibility.Collapsed;
            txtcirugias.Text = "";
            chkCirSi.IsChecked = false;
        }

        // =====================================================
        //                BOTÓN GUARDAR CITA
        // =====================================================

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string rutaCitas = @"C:\AppCitas\citas.json";

            // --- VALIDACIONES ---
            if (!await ValidarFormulario()) return;

            // --- OBTENER DATOS SEGUROS ---
            var itemDoctor = cbDoctores.SelectedItem as ComboBoxItem;
            var itemHora = cbHora.SelectedItem as ComboBoxItem;

            string doctorSeleccionado = itemDoctor?.Content?.ToString() ?? "";
            string horaSeleccionada = itemHora?.Content?.ToString() ?? "";
            DateTime fechaCita = dpCita.Date!.Value.DateTime;

            // --- VALIDAR DISPONIBILIDAD ---
            var citas = CargarCitas(rutaCitas);

            bool ocupado = citas.Any(c =>
                c.Doctor == doctorSeleccionado &&
                c.FechaCita.Date == fechaCita.Date &&
                c.Hora == horaSeleccionada);

            if (ocupado)
            {
                await MostrarMensaje("Horario Ocupado", "Este doctor ya tiene una cita reservada.");
                return;
            }

            // --- CREAR NUEVA CITA ---
            Cita nueva = new Cita
            { 
                Doctor = doctorSeleccionado,
                FechaCita = fechaCita,
                Hora = horaSeleccionada,
                Nombre = txtNombre.Text,
                Telefono = txtTelefono.Text,
                FechaNacimiento = dpNacimiento.Date!.Value.DateTime,
                TieneEnfermedades = chkEnfSi.IsChecked == true,
                Enfermedades = txtenfermedades.Text,
                TieneCirugias = chkCirSi.IsChecked == true,
                Cirugias = txtcirugias.Text,
                Medicamentos = txtMedicamentos.Text,
                Alergias = txtAlergias.Text,
                Motivo = txtMotivo.Text,
                FechaRegistro = DateTime.Now
            };

            // --- GUARDAR ---
            citas.Add(nueva);
            GuardarCitas(rutaCitas, citas);

            await MostrarMensaje("¡Logrado!", "Cita guardada correctamente.");

            LimpiarFormulario();
        }

        // =====================================================
        //                     VALIDACIONES
        // =====================================================

        private async Task<bool> ValidarFormulario()
        {
            if (cbDoctores.SelectedIndex <= 0)
            {
                await MostrarMensaje("Atención", "Debes seleccionar un doctor.");
                return false;
            }

            if (cbHora.SelectedIndex == 0)
            {
                await MostrarMensaje("Atención", "Debes seleccionar una hora.");
                return false;
            }

            if (!dpCita.Date.HasValue)
            {
                await MostrarMensaje("Atención", "Debes seleccionar una fecha.");
                return false;
            }

            if (dpCita.Date.Value.Date < DateTime.Today)
            {
                await MostrarMensaje("Atención", "No puedes seleccionar una fecha pasada.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text) || txtNombre.Text.Any(char.IsDigit))
            {
                await MostrarMensaje("Atención", "Nombre inválido.");
                return false;
            }

            string tel = txtTelefono.Text.Trim();
            if (tel.Length != 10 || !tel.All(char.IsDigit))
            {
                await MostrarMensaje("Atención", "Teléfono inválido.");
                return false;
            }

            string correo = txtCorreo.Text.Trim();
            string patron = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(correo, patron))
            {
                await MostrarMensaje("Atención", "El formato del correo electrónico no es válido.");
                return false;
            }

            if (!dpNacimiento.Date.HasValue || dpNacimiento.Date.Value.DateTime > DateTime.Now)
            {
                await MostrarMensaje("Error", "Fecha de nacimiento inválida.");
                return false;
            }

            if (chkEnfSi.IsChecked == false && chkEnfNo.IsChecked == false)
            {
                await MostrarMensaje("Atención", "Selecciona enfermedades.");
                return false;
            }

            if (chkCirSi.IsChecked == false && chkCirNo.IsChecked == false)
            {
                await MostrarMensaje("Atención", "Selecciona cirugías.");
                return false;
            }

            if (chkEnfSi.IsChecked == true && string.IsNullOrWhiteSpace(txtenfermedades.Text))
            {
                await MostrarMensaje("Atención", "Especifica enfermedades.");
                return false;
            }

            if (chkCirSi.IsChecked == true && string.IsNullOrWhiteSpace(txtcirugias.Text))
            {
                await MostrarMensaje("Atención", "Especifica cirugías.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtMotivo.Text))
            {
                await MostrarMensaje("Atención", "Motivo obligatorio.");
                return false;
            }

            return true;
        }

        // =====================================================
        //                  MÉTODOS AUXILIARES
        // =====================================================

        private List<Cita> CargarCitas(string ruta)
        {
            if (!File.Exists(ruta)) return new List<Cita>();

            string json = File.ReadAllText(ruta);
            return JsonSerializer.Deserialize<List<Cita>>(json) ?? new List<Cita>();
        }

        private void GuardarCitas(string ruta, List<Cita> citas)
        {
            // Configuramos las opciones para que se vea bien el JSON y acepte la 'ñ'
            var opciones = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
            };

            string json = JsonSerializer.Serialize(citas, opciones);

            File.WriteAllText(ruta, json);
        }

        private async Task MostrarMensaje(string titulo, string mensaje)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void LimpiarFormulario()
        {
            cbDoctores.SelectedIndex = 0;
            cbHora.SelectedIndex = 0;

            dpCita.Date = null;
            dpNacimiento.Date = null;

            txtNombre.Text = "";
            txtTelefono.Text = "";
            txtenfermedades.Text = "";
            txtcirugias.Text = "";
            txtMedicamentos.Text = "";
            txtAlergias.Text = "";
            txtMotivo.Text = "";

            chkEnfSi.IsChecked = false;
            chkEnfNo.IsChecked = false;
            chkCirSi.IsChecked = false;
            chkCirNo.IsChecked = false;

            txtenfermedades.Visibility = Visibility.Collapsed;
            txtcirugias.Visibility = Visibility.Collapsed;
        }

        private static void GuardarDatosLogin(string usuario, bool recordar)
        {
            try
            {
                string rutaLogin = @"C:\AppCitas\login.json";

                var datos = new DatosLogin
                {
                    Usuario = usuario,
                    UltimoAcceso = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    RecordarSesion = recordar
                };

                // Agregamos las opciones con el Encoder
                var opciones = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
                };

                string json = JsonSerializer.Serialize(datos, opciones);

                File.WriteAllText(rutaLogin, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al guardar login: " + ex.Message);
            }
        }
    }
}