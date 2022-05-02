using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SharpGL;
using SharpGL.Enumerations;


namespace LAB4
{
    public partial class Form1 : Form
    {
        private ArrayPoints frame = new ArrayPoints(2);
        private ArrayPoints segments = new ArrayPoints(2);
        int Xmin, Xmax, Ymin, Ymax; //максимальные значения окна
        OpenGL controller;
        public Form1()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Класс для хранения точек в списке
        /// </summary>
        public class ArrayPoints
        {
            private int index = 0;
            private List<Point> points;

            public ArrayPoints(int size)
            {
                if (size <= 0) { size = 2; }
                points = new List<Point>(2);
            }

            public void SetPoint(int x, int y)
            {

                points.Add(new Point(x, y));
                index++;
            }

            public void SetPoint(Point point)
            {
                points.Add(point);
                index++;
            }

            public void SetPoint(int position, Point point)
            {
                points.Insert(position, point);
                index++;
            }

            public void ResetPoints()
            {
                index = 0;
                points.Clear();
            }

            public int GetCountPoints()
            {
                return index;
            }

            public List<Point> GetPoints()
            {
                return points;
            }

            public void DeletePoint(int index)
            {
                points.RemoveAt(index);
                this.index--;
            }
        }

        private void openGLControl1_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
        {
            controller = this.openGLControl1.OpenGL;
            controller.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            controller.Color(1.0, 1.0, 1.0);
            controller.Begin(BeginMode.Lines);
            if (frame.GetCountPoints() > 0)
            {
                Point previous = frame.GetPoints().ToArray()[0];
                for (int i = 1; i < frame.GetCountPoints() + 1; i++)
                {
                    controller.Vertex(previous.X, previous.Y);
                    controller.Vertex(frame.GetPoints().ToArray()[frame.GetCountPoints() - i].X, frame.GetPoints().ToArray()[frame.GetCountPoints() - i].Y);
                    previous = frame.GetPoints().ToArray()[frame.GetCountPoints() - i];
                }
            }
            controller.End();

            controller.Color(0.0, 1.0, 0.0);
            controller.Begin(BeginMode.Lines);
            if (segments.GetCountPoints() > 0)
            {
                for (int i = 0; i < segments.GetCountPoints(); i++)
                {
                    controller.Vertex(segments.GetPoints().ToArray()[i].X, segments.GetPoints().ToArray()[i].Y);
                }
            }
            controller.End();

        }

        /// <summary>
        /// Генерирует квадратное окно по заданным параметрам
        /// </summary>
        /// <param name="sender">Кнопка</param>
        /// <param name="e">Клик</param>
        private void frameButton_Click(object sender, EventArgs e)
        {
            frame.ResetPoints(); //сбрасываем, если что-то было 

            //Добавляем опорные точки
            frame.SetPoint(int.Parse(xTextBox.Text), int.Parse(yTextBox.Text));
            frame.SetPoint(int.Parse(xTextBox.Text) + int.Parse(widthTextBox.Text), int.Parse(yTextBox.Text));
            frame.SetPoint(int.Parse(xTextBox.Text) + int.Parse(widthTextBox.Text), int.Parse(yTextBox.Text) + int.Parse(heightTextBox.Text));
            frame.SetPoint(int.Parse(xTextBox.Text), int.Parse(yTextBox.Text) + int.Parse(heightTextBox.Text));

            //Запоминаем наши максимальные и минимальные значения
            Xmin = int.Parse(xTextBox.Text);
            Xmax = int.Parse(xTextBox.Text) + int.Parse(widthTextBox.Text);
            Ymin = int.Parse(yTextBox.Text);
            Ymax = int.Parse(yTextBox.Text) + int.Parse(heightTextBox.Text);

            openGLControl1.DoRender();
        }

        /// <summary>
        /// Настраиваем камеру и отображение 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openGLControl1_Resized(object sender, EventArgs e)
        {
            var gl = this.openGLControl1.OpenGL;
            gl.MatrixMode(MatrixMode.Projection);
            gl.LoadIdentity();
            gl.Ortho(-799, 799, 390, -390, -450.0f, 450.0f);
            gl.MatrixMode(MatrixMode.Modelview);
            gl.LookAt(3.0, 3.0, 3.0 - 4.5, 0.0, 0.0, -4.5, 0, 1, 0);
            gl.LoadIdentity();
        }

        /// <summary>
        /// Генерирует отрезки (5 штук)
        /// </summary>
        /// <param name="sender">Кнопка</param>
        /// <param name="e">Клик</param>
        private void segmentButton_Click(object sender, EventArgs e)
        {
            Random random = new Random();
            for (int i = 0; i < 10; i += 2)
            {
                segments.SetPoint(random.Next(-799, 799), random.Next(-390, 390));
                segments.SetPoint(random.Next(-799, 799), random.Next(-390, 390));
            }

            openGLControl1.DoRender();
        }

        /// <summary>
        /// Очищает все с экрана
        /// </summary>
        /// <param name="sender">Кнопка</param>
        /// <param name="e">Клик</param>
        private void cleanButton_Click(object sender, EventArgs e)
        {
            frame.ResetPoints();
            segments.ResetPoints();
            openGLControl1.DoRender();
        }

        /// <summary>
        /// Реализаия алгоритма Коэна-Сазерленда для отрезания лишних отрезков
        /// </summary>
        /// <param name="sender">Кнопка</param>
        /// <param name="e">Клик</param>
        private void clipButton_Click(object sender, EventArgs e)
        {
            ArrayPoints oldSegments = CopyArrayPoints(segments); //здесь хранятся все точки до изменений
            int codeA, codeB, code; //Двоичные коды (левее, правее, выше, ниже)

            for (int i = 0; i < oldSegments.GetCountPoints(); i += 2) //Проверяем каждый отрезок (задается двумя точка, поэтому +2)
            {
                Point point;
                codeA = getCode(oldSegments.GetPoints().ToArray()[i].X, oldSegments.GetPoints().ToArray()[i].Y); //получаем код для первой точки
                codeB = getCode(oldSegments.GetPoints().ToArray()[i + 1].X, oldSegments.GetPoints().ToArray()[i + 1].Y); //получаем код для второй точки

                // пока одна из точек отрезка вне прямоугольника сидим внутри цикла
                while ((codeA | codeB) != 0)
                {
                    // если обе точки с одной стороны прямоугольника, то отрезок не пересекает прямоугольник
                    if ((codeA & codeB) != 0)
                    {
                        oldSegments.DeletePoint(i); //удаляем первую точку, но индекс уже изменился, поэтому следующая точка также имеет индекс i
                        oldSegments.DeletePoint(i); //удаляем вторую точку
                        i -= 2; //меняем нашу переменную цикла, ведь мы удалили
                        break; //выходим из цикла while, т.к. этот отрезок нам больше не нужен.
                    }

                    // выбираем точку c ненулевым кодом
                    if (codeA != 0)
                    {
                        code = codeA; //Либо первая точка, тогда получим ее координаты
                        point = oldSegments.GetPoints().ToArray()[i];
                    }
                    else
                    {
                        code = codeB; //Либо вторая точка, тогда получим ее координаты
                        point = oldSegments.GetPoints().ToArray()[i + 1];
                    }

                    //Если по коду получили, что точка левее, то передвигаем ее на прямую Xmin
                    //Если праве -- то на Xmax
                    if (point.X < Xmin)
                    {
                        if (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X != 0) //Не забываем проверять деление на ноль, а то будет плохо
                            point.Y += (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y) * (Xmin - point.X) / (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X);
                        else
                            point.Y += (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y) * (Xmin - point.X);
                        point.X = Xmin;
                    }
                    else if (point.X > Xmax)
                    {
                        if (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X != 0)
                            point.Y += (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y) * (Xmax - point.X) / (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X);
                        else
                            point.Y += (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y) * (Xmax - point.X);
                        point.X = Xmax;
                    }
                    //Если точка ниже прямоугольника, то передвигаем ее на Ymin
                    //Если выше -- На Ymax
                    else if (point.Y < Ymin)
                    {
                        if (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y != 0) // Все еще проверяем деление на ноль, а то будет боль
                            point.X += (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X) * (Ymin - point.Y) / (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y);
                        else
                            point.X += (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X) * (Ymin - point.Y);
                        point.Y = Ymin;
                    }
                    else if (point.Y > Ymax)
                    {
                        if (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y != 0)
                            point.X += (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X) * (Ymax - point.Y) / (oldSegments.GetPoints().ToArray()[i + 1].Y - oldSegments.GetPoints().ToArray()[i].Y);
                        else
                            point.X += (oldSegments.GetPoints().ToArray()[i + 1].X - oldSegments.GetPoints().ToArray()[i].X) * (Ymax - point.Y);
                        point.Y = Ymax;
                    }

                    //Точку сдвинули, но теперь нужно снова проверить бинарный код для этой точки
                    if (code == codeA)
                    {
                        oldSegments.DeletePoint(i);
                        oldSegments.SetPoint(i, point);
                        codeA = getCode(oldSegments.GetPoints().ToArray()[i].X, oldSegments.GetPoints().ToArray()[i].Y);
                    }
                    else
                    {
                        oldSegments.DeletePoint(i + 1);
                        oldSegments.SetPoint(i + 1, point);
                        codeB = getCode(oldSegments.GetPoints().ToArray()[i + 1].X, oldSegments.GetPoints().ToArray()[i + 1].Y);
                    }
                }
            }
            segments.ResetPoints(); //Сбрасываем все отрезки, что были у нас на экране
            segments = CopyArrayPoints(oldSegments); //Копируем в них те, что получили с помощью алгоритма выше
            openGLControl1.DoRender(); //Перерендериваем сцену
        }

        /// <summary>
        /// Получаем код, чтобы понять где находится точка относительно окна
        /// </summary>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <returns></returns>
        private int getCode(int x, int y)
        {
            return
                ((x < Xmin) ? 1 : 0) << 3 |
                ((x > Xmax) ? 1 : 0) << 2 |
                ((y < Ymin) ? 1 : 0) << 1 |
                ((y > Ymax) ? 1 : 0);
        }

        /// <summary>
        /// Копирует один ArrayPoints в другой ArrayPoints
        /// </summary>
        /// <param name="firstArray">Первый массив</param>
        /// <param name="secondArray">Второй массив</param>
        /// <returns></returns>
        private ArrayPoints CopyArrayPoints(ArrayPoints secondArray)
        {
            ArrayPoints firstArray = new ArrayPoints(secondArray.GetCountPoints());
            for (int i = 0; i < secondArray.GetCountPoints(); i++)
            {
                firstArray.SetPoint(secondArray.GetPoints().ToArray()[i]);
            }
            return firstArray;
        }


    }
}
