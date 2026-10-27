using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;

namespace sistema_de_calificaciones
{
    public partial class MainPage : ContentPage
    {
        private static readonly Color ColorAprobado = Color.FromArgb("#10B981");
        private static readonly Color ColorComplementario = Color.FromArgb("#F59E0B");
        private static readonly Color ColorReprobado = Color.FromArgb("#EF4444");
        private static readonly Color ColorPendiente = Color.FromArgb("#64748B");
        private static readonly Color ColorFoco = Color.FromArgb("#0284C7");

        private bool _animacionInicialLista = false;

        public MainPage()
        {
            InitializeComponent();
            dpFecha.Date = DateTime.Today;
        }

        private void OnEstudianteSelected(object sender, EventArgs e)
        {
            if (pickerEstudiantes.SelectedIndex != -1)
            {
                lblEstudianteSeleccionado.Text = pickerEstudiantes.SelectedItem.ToString();
                lblEstudianteSeleccionado.TextColor = Color.FromArgb("#0369A1");
            }
            else
            {
                lblEstudianteSeleccionado.Text = "Seleccionar alumno...";
                lblEstudianteSeleccionado.TextColor = Color.FromArgb("#94A3B8");
            }
        }

        // =====================================================
        //  ANIMACIÓN DE ENTRADA ESCALONADA
        // =====================================================
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_animacionInicialLista) return;
            _animacionInicialLista = true;

            var elementos = new VisualElement[]
            {
                header, cardEstudiante, cardParcial1, cardParcial2, cardResultado
            };

            foreach (var el in elementos)
            {
                el.Opacity = 0;
                el.TranslationY = 25;
                el.Scale = 0.96;
            }

            foreach (var el in elementos)
            {
                _ = Task.WhenAll(
                    el.FadeTo(1, 400, Easing.CubicOut),
                    el.TranslateTo(0, 0, 450, Easing.SpringOut),
                    el.ScaleTo(1.0, 400, Easing.CubicOut)
                );
                await Task.Delay(70);
            }
        }

        // =====================================================
        //  FOCO EN CAMPOS
        // =====================================================
        private async void OnCampoFocused(object sender, FocusEventArgs e)
        {
            if (sender is not VisualElement ve) return;
            var borde = BuscarBorde(ve);
            if (borde == null) return;

            borde.Stroke = new SolidColorBrush(ColorFoco);
            borde.StrokeThickness = 2;
            await borde.ScaleTo(1.015, 120, Easing.CubicOut);
        }

        private async void OnCampoUnfocused(object sender, FocusEventArgs e)
        {
            if (sender is not VisualElement ve) return;
            var borde = BuscarBorde(ve);
            if (borde == null) return;

            RestaurarBorde(borde);
            await borde.ScaleTo(1.0, 120, Easing.CubicIn);
        }

        private void OnNotaChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarParcialesEnVivo();
        }

        private void ActualizarParcialesEnVivo()
        {
            lblParcial1.Text = (ValidarNota(txtSeguimiento1.Text, out double s1) &&
                                ValidarNota(txtExamen1.Text, out double e1))
                ? ((s1 * 0.3) + (e1 * 0.2)).ToString("F2")
                : "0.00";

            lblParcial2.Text = (ValidarNota(txtSeguimiento2.Text, out double s2) &&
                                ValidarNota(txtExamen2.Text, out double e2))
                ? ((s2 * 0.3) + (e2 * 0.2)).ToString("F2")
                : "0.00";
        }

        // =====================================================
        //  CALCULAR Y ANIMACIÓN CONDICIONAL
        // =====================================================
        private async void OnCalcularClicked(object sender, EventArgs e)
        {
            await btnCalcular.ScaleTo(0.93, 80, Easing.CubicIn);
            await btnCalcular.ScaleTo(1.0, 120, Easing.SpringOut);

            if (pickerEstudiantes.SelectedIndex == -1)
            {
                var bordePicker = BuscarBorde(pickerEstudiantes);
                if (bordePicker != null) MarcarError(bordePicker);
                if (bordePicker != null) await Sacudir(bordePicker);
                await DisplayAlert("Error", "Debe seleccionar un estudiante.", "OK");
                if (bordePicker != null) RestaurarBorde(bordePicker);
                return;
            }

            var campos = new List<Entry> { txtSeguimiento1, txtExamen1, txtSeguimiento2, txtExamen2 };
            var invalidos = new List<Entry>();
            foreach (var campo in campos)
            {
                var borde = BuscarBorde(campo);
                if (!ValidarNota(campo.Text, out _))
                {
                    invalidos.Add(campo);
                    if (borde != null) MarcarError(borde);
                }
                else if (borde != null)
                {
                    RestaurarBorde(borde);
                }
            }

            if (invalidos.Count > 0)
            {
                foreach (var inv in invalidos)
                {
                    var b = BuscarBorde(inv);
                    if (b != null) _ = Sacudir(b);
                }
                await DisplayAlert("Error de Ingreso",
                    "Todas las notas deben ser numéricas y estar en el rango de 0.1 a 10.", "OK");
                return;
            }

            ValidarNota(txtSeguimiento1.Text, out double seg1);
            ValidarNota(txtExamen1.Text, out double ex1);
            ValidarNota(txtSeguimiento2.Text, out double seg2);
            ValidarNota(txtExamen2.Text, out double ex2);

            double parcial1 = (seg1 * 0.3) + (ex1 * 0.2);
            double parcial2 = (seg2 * 0.3) + (ex2 * 0.2);
            double notaFinal = parcial1 + parcial2;

            string estado;
            Color colorEstado;
            if (notaFinal >= 7.0)
            {
                estado = "APROBADO";
                colorEstado = ColorAprobado;
            }
            else if (notaFinal >= 5.0)
            {
                estado = "COMPLEMENTARIO";
                colorEstado = ColorComplementario;
            }
            else
            {
                estado = "REPROBADO";
                colorEstado = ColorReprobado;
            }

            // Animaciones base
            AnimarNumero(lblParcial1, parcial1);
            AnimarNumero(lblParcial2, parcial2);
            AnimarNumero(lblNotaFinal, notaFinal);
            AnimarMarcador(notaFinal, colorEstado);
            await AnimarBadgeEstado(estado, colorEstado);

            // ANIMACIÓN CONDICIONAL SEGÚN ESTADO (Aprobado / Reprobado)
            if (notaFinal >= 7.0)
            {
                // Animación de Celebración / Éxito (Aprobado)
                _ = cardResultado.ScaleTo(1.03, 160, Easing.CubicOut)
                   .ContinueWith(_ => cardResultado.ScaleTo(1.0, 200, Easing.SpringOut), TaskScheduler.FromCurrentSynchronizationContext());
                await cardResultado.RotateTo(1.5, 100, Easing.CubicOut);
                await cardResultado.RotateTo(-1.5, 100, Easing.CubicIn);
                await cardResultado.RotateTo(0, 100, Easing.CubicOut);
            }
            else if (notaFinal < 5.0)
            {
                // Animación de Alerta / Rechazo (Reprobado)
                await Sacudir(cardResultado);
            }
            else
            {
                // Animación de Advertencia (Complementario)
                await cardResultado.ScaleTo(1.02, 140, Easing.CubicOut);
                await cardResultado.ScaleTo(1.0, 140, Easing.CubicIn);
            }

            string fecha = dpFecha.Date.HasValue
                ? dpFecha.Date.Value.ToString("dd/MM/yyyy")
                : DateTime.Now.ToString("dd/MM/yyyy");

            string estudiante = pickerEstudiantes.SelectedItem?.ToString() ?? "";

            string mensaje = $"Nombre: {estudiante}\n" +
                             $"Fecha: {fecha}\n" +
                             $"Nota Parcial 1: {parcial1:F2}\n" +
                             $"Nota Parcial 2: {parcial2:F2}\n" +
                             $"Nota Final: {notaFinal:F2}\n" +
                             $"Estado: {estado}";

            await DisplayAlert("RESUMEN DE CALIFICACIÓN", mensaje, "Aceptar");
        }

        // =====================================================
        //  LIMPIAR
        // =====================================================
        private async void OnLimpiarClicked(object sender, EventArgs e)
        {
            await btnLimpiar.ScaleTo(0.93, 80, Easing.CubicIn);
            await btnLimpiar.ScaleTo(1.0, 100, Easing.CubicOut);

            await cardResultado.FadeTo(0.4, 150);

            pickerEstudiantes.SelectedIndex = -1;
            lblEstudianteSeleccionado.Text = "Seleccionar alumno...";
            lblEstudianteSeleccionado.TextColor = Color.FromArgb("#94A3B8");

            foreach (var campo in new[] { txtSeguimiento1, txtExamen1, txtSeguimiento2, txtExamen2 })
            {
                campo.Text = string.Empty;
                var borde = BuscarBorde(campo);
                if (borde != null) RestaurarBorde(borde);
            }

            this.AbortAnimation("marcador");
            marcador.TranslationX = 0;
            lblParcial1.Text = "0.00";
            lblParcial2.Text = "0.00";
            lblNotaFinal.Text = "0.00";
            lblEstado.Text = "PENDIENTE";
            badgeEstado.BackgroundColor = ColorPendiente;
            dpFecha.Date = DateTime.Today;

            await cardResultado.FadeTo(1.0, 200);
        }

        // =====================================================
        //  MÉTODOS DE ANIMACIÓN
        // =====================================================
        private void AnimarNumero(Label etiqueta, double destino)
        {
            string nombre = $"contar_{etiqueta.GetHashCode()}";
            this.AbortAnimation(nombre);
            this.Animate(nombre,
                v => etiqueta.Text = v.ToString("F2"),
                0, destino, length: 650, easing: Easing.CubicOut);
        }

        private void AnimarMarcador(double nota, Color colorEstado)
        {
            double ancho = barraGrid.Width;
            if (ancho <= 0) return;

            double recorrido = ancho - marcador.WidthRequest;
            double destino = Math.Max(0, recorrido * (nota / 10.0));

            marcador.Stroke = new SolidColorBrush(colorEstado);
            this.AbortAnimation("marcador");
            this.Animate("marcador",
                v => marcador.TranslationX = v,
                marcador.TranslationX, destino, length: 750, easing: Easing.CubicInOut);
        }

        private async Task AnimarBadgeEstado(string estado, Color colorEstado)
        {
            lblEstado.Text = estado;
            badgeEstado.BackgroundColor = colorEstado;

            badgeEstado.Scale = 0.5;
            badgeEstado.Rotation = -5;
            _ = badgeEstado.RotateTo(0, 300, Easing.CubicOut);
            await badgeEstado.ScaleTo(1.0, 350, Easing.SpringOut);
        }

        private static async Task Sacudir(VisualElement el)
        {
            uint t = 45;
            await el.TranslateTo(-10, 0, t);
            await el.TranslateTo(10, 0, t);
            await el.TranslateTo(-7, 0, t);
            await el.TranslateTo(7, 0, t);
            await el.TranslateTo(-3, 0, t);
            await el.TranslateTo(3, 0, t);
            await el.TranslateTo(0, 0, t);
        }

        private static Border? BuscarBorde(Element? el)
        {
            while (el != null && el is not Border)
                el = el.Parent;
            return el as Border;
        }

        private static void MarcarError(Border borde)
        {
            borde.Stroke = new SolidColorBrush(ColorReprobado);
            borde.StrokeThickness = 2;
        }

        private static void RestaurarBorde(Border borde)
        {
            borde.Stroke = new SolidColorBrush(Color.FromArgb("#38BDF8"));
            borde.StrokeThickness = 1.5;
        }

        private bool ValidarNota(string? input, out double valor)
        {
            valor = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim().Replace(',', '.');
            bool esValido = double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out valor);
            return esValido && valor >= 0.1 && valor <= 10.0;
        }
    }
}