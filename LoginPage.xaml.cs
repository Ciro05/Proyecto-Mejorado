using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Formulario_1
{
    public sealed partial class LoginPage : Page
    {
        // --- CONSTRUCTOR ---

        public LoginPage()
        {
            this.InitializeComponent();
        }



        // =====================================================
        //                    INICIO DE SESIÓN
        // =====================================================
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsuario.Text;
            string pass = txtPassword.Password;

            // Validación básica
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                await MostrarMensaje("Atención", "Por favor, completa ambos campos.");
                return;
            }

            string resultado = ObtenerResultadoValidacion(user, pass);

            if (resultado == "OK")
            {
                GuardarDatosLogin(user, true);
                this.Frame.Navigate(typeof(FormularioPage));
            }
            else
            {
                await MostrarMensaje("Error de Acceso", resultado);
            }
        }


        // =====================================================
        //                    VER REPORTES (ADMIN)
        // =====================================================
        private async void VerReportes_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsuario.Text;
            string pass = txtPassword.Password;

            if (EsAdminValido(user, pass))
            {
                this.Frame.Navigate(typeof(ReportsPage));
            }
            else
            {
                await MostrarMensaje("Acceso denegado", "Solo administradores pueden ver reportes.");
            }
        }

        // =====================================================
        //          PANEL ADMIN (REGISTRAR / ELIMINAR)
        // =====================================================
        private async void btnIrARegistro_Click(object sender, RoutedEventArgs e)
        {
            var loginAdmin = await MostrarDialogoAdmin();

            // Verifica si se ingresaron credenciales
            if (loginAdmin != null)
            {
                if (EsAdminValido(loginAdmin.Value.User, loginAdmin.Value.Pass))
                {
                    await MostrarOpcionesAdmin();
                }
                else
                {
                    await MostrarMensaje("Acceso Denegado",
                        "Se requieren credenciales de Administrador.");
                }
            }
        }

        // --- MENÚ DE OPCIONES ADMIN ---
        private async Task MostrarOpcionesAdmin()
        {
            var dialog = new ContentDialog
            {
                Title = "Administración de Usuarios",
                Content = "Selecciona una opción",
                PrimaryButtonText = "Agregar Usuario",
                SecondaryButtonText = "Eliminar Usuario",
                CloseButtonText = "Salir",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await MostrarDialogoNuevoUsuario();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await MostrarDialogoEliminarUsuario();
            }
            else
            {
                await ConfirmarSalidaAdmin();
            }
        }

        // --- CONFIRMAR SALIDA DEL PANEL ADMIN ---
        private async Task ConfirmarSalidaAdmin()
        {
            var confirm = new ContentDialog
            {
                Title = "Confirmar salida",
                Content = "¿Seguro que deseas salir del panel de administración?",
                PrimaryButtonText = "Sí",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                this.Frame.Navigate(typeof(FormularioPage));
            }
            else
            {
                await MostrarOpcionesAdmin();
            }
        }

        // =====================================================
        //                  ELIMINAR USUARIO
        // =====================================================
        private async Task MostrarDialogoEliminarUsuario()
        {
            string ruta = @"C:\AppCitas\usuarios.json";

            if (!File.Exists(ruta))
            {
                await MostrarMensaje("Error", "No hay usuarios registrados.");
                return;
            }

            var lista = JsonSerializer.Deserialize<List<UsuarioAcceso>>(File.ReadAllText(ruta))
                        ?? new List<UsuarioAcceso>();

            var combo = new ComboBox { Header = "Selecciona usuario" };

            foreach (var u in lista)
            {
                combo.Items.Add(u.Usuario);
            }

            var dialog = new ContentDialog
            {
                Title = "Eliminar Usuario",
                Content = combo,
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            dialog.PrimaryButtonClick += async (s, e) =>
            {
                e.Cancel = true;

                if (combo.SelectedItem is not string usuarioEliminar)
                {
                    await MostrarMensaje("Error", "Selecciona un usuario.");
                    return;
                }

                lista.RemoveAll(u => u.Usuario == usuarioEliminar);

                File.WriteAllText(ruta, JsonSerializer.Serialize(lista,
                    new JsonSerializerOptions { WriteIndented = true }));

                dialog.Hide();

                await MostrarMensaje("Éxito", "Usuario eliminado correctamente.");

                await MostrarOpcionesAdmin();
            };

            await dialog.ShowAsync();
        }

        // =====================================================
        //                 REGISTRAR USUARIO
        // =====================================================
        private async Task MostrarDialogoNuevoUsuario()
        {
            var newUser = new TextBox { Header = "Nuevo Usuario" };
            var newPass = new PasswordBox { Header = "Nueva Contraseña" }; // Cambiado a PasswordBox por seguridad

            var newRol = new ComboBox { Header = "Rol" };
            newRol.Items.Add("Admin");
            newRol.Items.Add("Doctor");
            newRol.Items.Add("Recepcion");
            newRol.SelectedIndex = 1;

            // Este es tu mensajero de errores interno
            var txtErrorInterno = new TextBlock
            {
                Foreground = new SolidColorBrush(Colors.Red),
                Text = "",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            };

            var stack = new StackPanel
            {
                Children = { newUser, newPass, newRol, txtErrorInterno },
                Spacing = 10
            };

            var dialog = new ContentDialog
            {
                Title = "Registrar Nuevo Usuario",
                Content = stack,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            dialog.PrimaryButtonClick += async (s, e) =>
            {
                e.Cancel = true; 
                try
                {
                    txtErrorInterno.Text = ""; 

                    if (string.IsNullOrWhiteSpace(newUser.Text))
                    {
                        txtErrorInterno.Text = "El nombre de usuario es obligatorio.";
                        return;
                    }

                    string password = newPass.Password; 

                    if (password.Length < 6)
                    {
                        txtErrorInterno.Text = "La contraseña debe tener al menos 6 caracteres.";
                        return;
                    }

                    if (!password.Any(char.IsDigit))
                    {
                        txtErrorInterno.Text = "La contraseña debe incluir al menos un número.";
                        return;
                    }

                    GuardarNuevoUsuario(newUser.Text, password, (string)newRol.SelectedItem);

                    dialog.Hide(); 

                    await Task.Delay(300);
                    await MostrarMensaje("Éxito", "Usuario creado correctamente.");
                }
                catch (Exception ex)
                {
                    txtErrorInterno.Text = ex.Message;
                }
            };

            await dialog.ShowAsync();
        }

        // Método auxiliar para evitar el choque de diálogos al terminar
        private async Task MostrarMensajeExitoRetrasado()
        {
            await Task.Delay(500); // Esperamos a que el primero termine de animar su cierre
            await MostrarMensaje("Éxito", "Usuario registrado correctamente.");
            await MostrarOpcionesAdmin();
        }

        // --- VALIDAR USUARIO ---
        private bool ValidarUsuario(string user)
        {
            if (string.IsNullOrWhiteSpace(user)) return false;

            // Permite letras, espacios, punto y guion
            return user.All(c =>
                char.IsLetter(c) || c == ' ' || c == '.' || c == '-');
        }

        // =====================================================
        //                   JSON / DATOS
        // =====================================================

        private string ObtenerResultadoValidacion(string user, string pass)
        {
            string ruta = @"C:\AppCitas\usuarios.json";

            // Crear archivo si no existe
            if (!File.Exists(ruta))
            {
                var listaInicial = new List<UsuarioAcceso>
                {
                    new UsuarioAcceso { Usuario = "admin", Password = "123", Rol = "Admin" }
                };

                File.WriteAllText(ruta, JsonSerializer.Serialize(listaInicial,
                    new JsonSerializerOptions { WriteIndented = true }));
            }

            try
            {
                var usuarios = JsonSerializer.Deserialize<List<UsuarioAcceso>>(File.ReadAllText(ruta));

                if (usuarios == null) return "Error al leer la base de datos.";

                var cuenta = usuarios.FirstOrDefault(u => u.Usuario == user);

                if (cuenta == null) return "El usuario no existe.";

                return (cuenta.Password == pass) ? "OK" : "Contraseña incorrecta.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "Error técnico.";
            }
        }

        private bool EsAdminValido(string user, string pass)
        {
            string ruta = @"C:\AppCitas\usuarios.json";

            if (!File.Exists(ruta)) return false;

            try
            {
                var usuarios = JsonSerializer.Deserialize<List<UsuarioAcceso>>(File.ReadAllText(ruta));

                return usuarios != null &&
                       usuarios.Any(u => u.Usuario == user &&
                                         u.Password == pass &&
                                         u.Rol == "Admin");
            }
            catch
            {
                return false;
            }
        }

        private void GuardarNuevoUsuario(string user, string pass, string rol)
        {
            try
            {
                string ruta = @"C:\AppCitas\usuarios.json";

                var lista = JsonSerializer.Deserialize<List<UsuarioAcceso>>(File.ReadAllText(ruta))
                            ?? new List<UsuarioAcceso>();

                bool existe = lista.Any(u =>
                    u.Usuario.Equals(user, StringComparison.OrdinalIgnoreCase));

                if (existe)
                {
                    throw new Exception("El usuario ya existe.");
                }

                if (pass.Length < 6)
                {
                    throw new Exception("La contraseña debe tener al menos 6 caracteres.");
                }

                if (!pass.Any(char.IsDigit))
                {
                    throw new Exception("La contraseña debe contener al menos un número.");
                }

                lista.Add(new UsuarioAcceso
                {
                    Usuario = user,
                    Password = pass,
                    Rol = rol
                });

                var opciones = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
                };

                File.WriteAllText(ruta, JsonSerializer.Serialize(lista, opciones));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private void GuardarDatosLogin(string usuario, bool recordar)
        {
            try
            {
                string ruta = @"C:\AppCitas\login.json";

                var datos = new DatosLogin
                {
                    Usuario = usuario,
                    UltimoAcceso = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    RecordarSesion = recordar
                };

                File.WriteAllText(ruta, JsonSerializer.Serialize(datos,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // =====================================================
        //                    UI AUXILIAR
        // =====================================================
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

        private async Task<(string User, string Pass)?> MostrarDialogoAdmin()
        {
            var userBox = new TextBox { Header = "Usuario Admin" };
            var passBox = new PasswordBox { Header = "Contraseña Admin" };

            var stack = new StackPanel
            {
                Children = { userBox, passBox },
                Spacing = 10
            };

            var dialog = new ContentDialog
            {
                Title = "Autorización Requerida",
                Content = stack,
                PrimaryButtonText = "Autorizar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary
                ? (userBox.Text, passBox.Password)
                : null;
        }
    }
}