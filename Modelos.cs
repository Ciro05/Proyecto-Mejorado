using System;

namespace Formulario_1
{
    // =========================================================
    //                 MODELO: CITA MÉDICA
    //         Representa toda la información de una cita
    // =========================================================
    public class Cita
    {
        // --- DATOS DE LA CITA ---
        public string Doctor { get; set; } = "";          // Nombre del doctor asignado
        public DateTime FechaCita { get; set; }           // Fecha de la cita
        public string Hora { get; set; } = "";            // Hora seleccionada

        // --- DATOS DEL PACIENTE ---
        public string Nombre { get; set; } = "";          // Nombre completo del paciente
        public string Telefono { get; set; } = "";        // Teléfono (10 dígitos)
        public DateTime FechaNacimiento { get; set; }     // Fecha de nacimiento

        // --- HISTORIAL MÉDICO ---
        public bool TieneEnfermedades { get; set; }       // Indicador de enfermedades crónicas
        public string Enfermedades { get; set; } = "";    // Detalle de enfermedades

        public bool TieneCirugias { get; set; }           // Indicador de cirugías previas
        public string Cirugias { get; set; } = "";        // Detalle de cirugías

        public string Medicamentos { get; set; } = "";    // Medicamentos actuales
        public string Alergias { get; set; } = "";        // Alergias del paciente

        // --- INFORMACIÓN ADICIONAL ---
        public string Motivo { get; set; } = "";          // Motivo de la consulta
        public DateTime FechaRegistro { get; set; }       // Fecha en que se registró la cita
    }

    // =========================================================
    //                 MODELO: DATOS DE LOGIN
    //          Guarda información de sesión del usuario
    // =========================================================
    public class DatosLogin
    {
        public string Usuario { get; set; } = "";         // Usuario que inició sesión
        public string UltimoAcceso { get; set; } = "";    // Fecha y hora del último acceso
        public bool RecordarSesion { get; set; }          // Indica si se guarda la sesión
    }

    // =========================================================
    //              MODELO: USUARIO DEL SISTEMA
    //  Maneja credenciales y roles (Admin, Doctor, Recepción)
    // =========================================================
    public class UsuarioAcceso
    {
        public string Usuario { get; set; } = "";         // Nombre de usuario
        public string Password { get; set; } = "";        // Contraseña
        public string Rol { get; set; } = "";             // Rol (Admin, Doctor, Recepcion)
    }
}