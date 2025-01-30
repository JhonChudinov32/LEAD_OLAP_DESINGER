using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace LEAD_OLAP_DESINGER.Class
{
    public class LineControl : Control
    {
        private Point _fromPoint, _toPoint, _fromLinePoint, _toLinePoint;
        private Color _brushColor;
        private double _brushWidth;

        public bool IsDirected { get; set; }
        public int DirectionSize { get; set; } = 7;

        public LineControl()
        {
            ClipToBounds = false; // Позволяет линиям выходить за пределы элемента
        }

        // Установка координат для линии
        public void SetCoordinates(int fromX, int fromY, int toX, int toY)
        {
            var left = Math.Min(fromX, toX);
            var top = Math.Min(fromY, toY);
            var width = Math.Abs(toX - fromX);
            var height = Math.Abs(toY - fromY);

            // Проверка, чтобы линия не была невидимой
            if (width == 0 || height == 0)
                return;

            Margin = new Thickness(left, top, 0, 0);
            Width = Math.Max(width, 1); // Минимальная ширина
            Height = Math.Max(height, 1); // Минимальная высота

            _fromPoint = new Point(fromX >= toX ? Width : 0, fromY >= toY ? Height : 0);
            _toPoint = new Point(fromX < toX ? Width : 0, fromY < toY ? Height : 0);

            _fromLinePoint = new Point(
                _fromPoint.X + (fromX >= toX ? -DirectionSize : DirectionSize),
                _fromPoint.Y);
            _toLinePoint = new Point(
                _toPoint.X + (fromX < toX ? -DirectionSize : DirectionSize),
                _toPoint.Y);

            InvalidateVisual(); // Перерисовать элемент
        }

        // Инициализация линии
        public void Initialize(Control parentControl, int fromX, int fromY, int toX, int toY, Color lineColor, double lineWidth, bool isDirected)
        {
            if (parentControl == null)
                throw new ArgumentNullException(nameof(parentControl), "Parent control cannot be null.");

            _brushColor = lineColor;
            _brushWidth = lineWidth;
            IsDirected = isDirected;

            SetCoordinates(fromX, fromY, toX, toY);

            if (parentControl is Panel panel)
            {
                // Добавляем линию в родительский элемент
                panel.Children.Add(this);
            }
         

            // Перерисовываем родительский элемент
            parentControl.InvalidateVisual();
        }

        // Удаление линии
        public void Remove()
        {
            // Удаление элемента из родительского контейнера
            if (Parent is Panel parent)
            {
                parent.Children.Remove(this);
            }
        }

        // Рендеринг линии
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Width <= 0 || Height <= 0) return;

            var pen = new Pen(new SolidColorBrush(_brushColor), _brushWidth); // Использование цвета для линии

            if (IsDirected)
            {
                // Рисуем линии с направлением
                context.DrawLine(pen, _fromPoint, _fromLinePoint);
                context.DrawLine(pen, _fromLinePoint, _toLinePoint);
                context.DrawLine(pen, _toLinePoint, _toPoint);
            }
            else
            {
                // Простая линия
                context.DrawLine(pen, _fromPoint, _toPoint);
            }
        }

        // Упрощённый конструктор
        public LineControl(Control parentControl, int fromX, int fromY, int toX, int toY, Color lineColor, double lineWidth, bool isDirected)
            : this()
        {
            Initialize(parentControl, fromX, fromY, toX, toY, lineColor, lineWidth, isDirected);
        }
    }
 
}
