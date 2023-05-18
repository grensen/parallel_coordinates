using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using System.Linq; //using System.Globalization; //using System.Windows.Documents;
using System.Xml.Linq;
//using System.Printing;
//using System.Security.Cryptography.Xml;

public class TheWindow : Window
{

    // colors
    readonly SolidColorBrush font = new(Color.FromRgb(230, 230, 230));
    // support 
    readonly System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.GetCultureInfo("en-us");
    readonly Typeface tf = new("TimesNewRoman"); // "Arial" // TimesNewRoman

    // layout
    Canvas canGlobal = new Canvas(),
                canVisual = new(), 
                canCurrent = new(),  
            canVisualBackground = new();

    double fs = 0; // feature size
    double height = 700;
    int dataStateCnt = 0;
    int features = 0;
    int labelNum = 0;
    int ys = 30; // y start point
    int xs = 20; // x start point menu
    float[] minVal = new float[49], maxVal = new float[49];
    bool[] featureState = new bool[49];
    bool[] isLabel = new bool[10];
    float[] trainData;
    int[] trainLabels;

    int styleCnt = 0;
    int ggCount = 0; //#
    bool dataShuffle = false;
    Brush[] br = new Brush[10];
    string name = "";

    [STAThread]
    public static void Main() { new Application().Run(new TheWindow()); }

    // CONSTRUCTOR - LOADED - ONINIT
    private TheWindow() // constructor
    {
        // set window   
        Title = "Parallel coordintes 23"; //        
        Content = canGlobal;
        Background = RGB(0, 0, 0);
        Width = 960; // 24 + 10 + 5 + 5 + 30 + 28*9 + 600 + 100 + 30; // WidthG;
        Height = 540; // HeightG;                   

        MouseDown += Mouse_Down;
        SizeChanged += Window_SizeChanged;  

        canGlobal.Children.Add(canVisualBackground);
        canGlobal.Children.Add(canVisual);
        canGlobal.Children.Add(canCurrent);

        DataInit();
        ColorInit();
        return;
        // continue in Window_SizeChanged()...
    } // TheWindow end
    void Mouse_Down(object sender, MouseButtonEventArgs e)
    {

        int gpy = (int)e.GetPosition(this).Y, gpx = (int)e.GetPosition(this).X;

        ButtonActions();

        void ButtonActions()
        {
            if (gpy > ys + 0 && gpy < ys + 0 + 20 && gpx > xs && gpx < xs + 20)
            {
                dataStateCnt++;
                DataInit();
                DrawParallelCoordinates();
            }
            for (int i = 0; i < labelNum; i++)
            {
                if (gpy > ys + 30 + i * 30 && gpy < ys + 30 + i * 30 + 20 && gpx > xs && gpx < xs + 20)
                {
                    isLabel[i] = !isLabel[i];
                    DrawParallelCoordinates(); break;
                }
            }
            int ys2 = labelNum * 30; // class dist y for n classes * 30 pixels 
            if (gpy > ys + ys2 + 30 && gpy < ys + ys2 + 30 + 20 && gpx > xs && gpx < xs + 20)
            {
                // todo all on or off
                DrawParallelCoordinates();
            }
            if (gpy > ys + ys2 + 60 && gpy < ys + ys2 + 60 + 20 && gpx > xs && gpx < xs + 20)
            {
                styleCnt++;
                DrawParallelCoordinates();
            }
            if (gpy > ys + ys2 + 90 && gpy < ys + ys2 + 90 + 20 && gpx > xs && gpx < xs + 20)
            {
                dataShuffle = true;
                DrawParallelCoordinates();
                dataShuffle = !true;
            }

            // on off features
            int inputLen = trainData.Length / trainLabels.Length;
            for (int i = 0; i < inputLen; i++)
            {
                if (gpy > ys + ys2 + height + 25 && gpy < ys + ys2 + height + 25 + 20 && gpx > xs + i * fs && gpx < xs + i * fs + 20)
                {
                    featureState[i] = !featureState[i];
                    DrawParallelCoordinates(); break;
                }
            }
            // local helper
            void DrawParallelCoordinates()
            {
                DrawingContext dc = ContextHelpMod(false, ref canVisualBackground);
                ParallelCoordinatesVisual(ref dc);
                dc.Close();
            }
        }
    }
    void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        fs = (((Canvas)this.Content).RenderSize.Width - 30) / features;
        height = ((Canvas)this.Content).RenderSize.Height - 80;

        DrawingContext dc = ContextHelpMod(false, ref canVisualBackground);
        ParallelCoordinatesVisual(ref dc);
        dc.Close();

    } // Window_SizeChanged end

    void ParallelCoordinatesVisual(ref DrawingContext dc)
    {
        // return;
        int inputLen = trainData.Length / trainLabels.Length;
        int len = trainLabels.Length >= 10000 ? 10000 * inputLen : trainData.Length;
        BackgroundStuff(ref dc, inputLen, len);

        int[] ids = new int[trainLabels.Length];
        Shuffle(ids, dataShuffle, new Random(ggCount++));        

        styleCnt = styleCnt > 2 ? 0 : styleCnt;

        for (int i = 0, lb = 0; i < len; i += inputLen, lb++)
        {
            var lab = trainLabels[ids[lb]];
            int id = ids[lb] * inputLen;
            var clr = br[lab];
          //  if (lab == ccnt) continue;
            if (!isLabel[lab]) continue;
            var startLine = ys + lab * (height / (labelNum - 1));

            if (styleCnt == 0)
                for (int j = 0; j < inputLen; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = ys + height - height * ((trainData[id + j] - minVal[j]) / (maxVal[j] - minVal[j]));
                    Line(ref dc, clr, 0.5, 15 + j * fs, startLine, 15 + (j + 1) * fs, pixelPos); // dc.DrawLine(new Pen(clr, 0.5), new Point(15 + j * fs, ys + startLine), new Point(15 + (j + 1) * fs, ys + pixelPos));
                    startLine = pixelPos;
                }
            else if (styleCnt == 1)
                for (int j = 0; j < inputLen; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = ys + height - height * ((trainData[id + j] - minVal[j]) / (maxVal[j] - minVal[j]));
                    Line(ref dc, clr, 0.5, 15 + j * fs, startLine, 15 + (j + 1) * fs, pixelPos); // dc.DrawLine(new Pen(clr, 0.5), new Point(15 + j * fs, ys + startLine), new Point(15 + (j + 1) * fs, ys + pixelPos));
                }
            else if (styleCnt == 2)
                for (int j = 0; j < inputLen; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = ys + height - height * ((trainData[id + j] - minVal[j]) / (maxVal[j] - minVal[j]));
                    Line(ref dc, clr, 0.5, 15 + j * fs, pixelPos, 15 + (j + 1) * fs, pixelPos); // dc.DrawLine(new Pen(clr, 0.5), new Point(15 + j * fs, ys + pixelPos), new Point(15 + (j + 1) * fs, ys + pixelPos));
                }
        }

        // values max min visual plus lines  
        for (int j = 0; j < features; j++)
        {
            dc.DrawLine(new Pen(font, 1.0), new Point(15 + (j + 1) * fs, ys), new Point(15 + (j + 1) * fs, ys + height));
            double range = maxVal[j] - minVal[j];
            for (int i = 0; i < 20 + 1; i++) // accuracy lines 0, 20, 40...
            {
                double yGrph = ys + height - i * (height / 20.0f);
                Line(ref dc, font, 1.0, 15 + (j + 1) * fs - 5, yGrph, 15 + (j + 1) * fs, yGrph);
                Text(ref dc, (range / 10.0f * i + minVal[j]).ToString("F2"), 8, font, (int)((j + 1) * fs - 10), (int)yGrph - 5);
            }
        }

        InfoStuff(ref dc, inputLen, ys);


        void InfoStuff(ref DrawingContext dc, int inputLen, int ys)
        {
            Text(ref dc, "Data", 10, font, xs + 30, ys + 5); // switch data
            Rect(ref dc, RGB(64, 64, 64), xs, ys + 0, 20, 20);// switch data button

            for (int i = 0; i < labelNum; i++, ys += 30)
            {
                Text(ref dc, "Class " + i.ToString(), 10, br[i], xs + 30, ys + 35);
                Rect(ref dc, RGB(64, 64, 64), xs - 1, ys + 29, 22, 22); // background class button
                Rect(ref dc, (isLabel[i] ? br[i] : RGB(64, 64, 64)), xs, ys + 30, 20, 20); // class button
            }

            Text(ref dc, "All", 10, font, xs + 30, ys + 35);
            Text(ref dc, "Style", 10, font, xs + 30, ys + 65);
            Text(ref dc, "Shuffle", 10, font, xs + 30, ys + 95);

            Rect(ref dc, RGB(64, 64, 64), xs, ys + 30, 20, 20); // All
            Rect(ref dc, RGB(192, 128, 192), xs, ys + 60, 20, 20); // Style
            Rect(ref dc, RGB(184, 235, 184), xs, ys + 90, 20, 20); // Shuffle

            for (int i = 0; i < inputLen; i++)
                Rect(ref dc, featureState[i] ? RGB(164, 64, 164) : RGB(16, 16, 16), (int)(xs + i * fs), (int)(ys + height + 25), 20, 20);
        }

        void BackgroundStuff(ref DrawingContext dc, int inputLen, int len)
        {
            Text(ref dc, name + " dataset with " + (len / inputLen).ToString() + " examples, " + inputLen.ToString()
                + " features and " + labelNum.ToString() + " labels", 12, font, (int)(15 + 0 * fs), 5);

            Rect(ref dc, RGB(24, 24, 24), 15, ys, (int)(inputLen * fs), (int)height);
            // feature id's
            for (int i = 0; i < inputLen; i++)
                Text(ref dc, i.ToString(), 8, Brushes.White, (int)(15 + i * fs), 20);
        }
        void Line(ref DrawingContext dc, Brush rgb, double size, double xl, double yl, double xr, double yr)
        {
            dc.DrawLine(new Pen(rgb, size), new Point(xl, yl), new Point(xr, yr));
        }
        void Text(ref DrawingContext dc, string str, int size, Brush rgb, int x, int y)
        {
            dc.DrawText(new FormattedText(str, ci, FlowDirection.LeftToRight, tf, size, rgb, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
        }
        void Rect(ref DrawingContext dc, Brush rgb, int x, int y, int width, int height)
        {
            dc.DrawRectangle(rgb, null, new Rect(x, y, width, height));
        }
        static void Shuffle(int[] route, bool shuffle, Random rn)
        {
            int n = route.Length;
            for (int i = 0; i < n; i++) route[i] = i;

            if (shuffle)
                for (int i = 0, r = rn.Next(0, n); i < n; i++, r = rn.Next(i, n))
                    (route[r], route[i]) = (route[i], route[r]);
        }
    }
    // EVENT 

    void DataInit()
    {
        trainData = null; trainLabels = null;
        dataStateCnt = dataStateCnt > 3 ? 0 : dataStateCnt;

        if (dataStateCnt == 2) // mnist
        {        
            name = "mnist_train";        
            labelNum = 10;
            string dataPath = @"C:\mnist\trainData",  labelPath = @"C:\mnist\trainLabel";
            int num = 20000;
            var byteData = File.ReadAllBytesAsync(dataPath).Result.Take(num * 784).ToArray();
            var byteLabel = File.ReadAllBytesAsync(labelPath).Result.Take(num).ToArray();


            if (true)
            {
                int[] dimSteps = {4, 7, 14, 2}; // dimSteps * dimSteps = pooling map
                int dim = dimSteps[0];
                features = (28 / dim) * (28 / dim);
                trainData = new float[num * features];
                trainLabels = new int[num];
                
                for (int ex = 0, c = 0; ex < num; ex++)
                    for (int y = 0, id = ex * 784; y < 28; y += dim)
                        for (int x = 0; x < 28; x += dim, c++)
                        {
                            float sum = 0;
                            for (int i = 0, i2 = id + x + y * 28; i < dim; i++)
                                for (int j = 0; j < dim; j++)
                                    sum += (float)byteData[i2 + i * 28 + j] / 255f;
                            trainData[c] = sum;
                        }
            
            }

            for (int i = 0; i < trainLabels.Length; i++)
                trainLabels[i] = byteLabel[i];          
        }

        if (dataStateCnt == 0) // iris_train
        {
            name = "iris_train";
            labelNum = 3;
            features = 4;
            var trainPath = @"C:\datasets\iris_train.txt";
            trainData = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).SelectMany(values => values.Skip(0).Take(4).Select(float.Parse)).ToArray();
            trainLabels = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).Select(values => int.Parse(values.Last())).ToArray();
     
        }
        if (dataStateCnt == 1) // higgs_train_800
        {
            name = "higgs_train_800";
            labelNum = 2;
            features = 30;
            var trainPath = @"C:\datasets\higgs_train_800.txt";
            trainData = File.ReadAllLines(trainPath).Skip(5).Select(line => line.Split(',')).SelectMany(values => values.Skip(1).Take(30).Select(float.Parse)).ToArray();
            trainLabels = File.ReadAllLines(trainPath).Skip(5).Select(line => line.Split(',')).Select(values => int.Parse(values.Last())).ToArray();
        }
        if (dataStateCnt == 3)
        {
            name = "creditcard_10k";
            labelNum = 2;
            features = 28;
            int maxLines = 20000;
            var trainPath = @"C:\datasets\creditcard.txt";
            trainData = File.ReadAllLines(trainPath).Skip(1).Take(maxLines).Select(line => line.Split(',')).SelectMany(values => values.Skip(1).Take(28).Select(float.Parse)).ToArray();
            trainLabels = File.ReadAllLines(trainPath).Skip(1).Take(maxLines).Select(line => line.Split(',')).Select(values => int.Parse(values.Last().Trim('"'))).ToArray();
        }

        minVal = new float[features];
        maxVal = new float[features];
        featureState = new bool[features];
        for (int vals = 0; vals < features; vals++)
        {
            float cmin = float.MaxValue, cmax = float.MinValue;
            for (int dt = 0; dt < trainLabels.Length; dt++)
            {
                var val = trainData[vals + features * dt];
                if (val < cmin) { cmin = val; }
                if (val > cmax) { cmax = val; }
            }
            minVal[vals] = cmin; maxVal[vals] = cmax;
        }
        for (int i = 0; i < features; i++) featureState[i] = true;
        isLabel = new bool[labelNum];
        for (int i = 0; i < labelNum; i++) isLabel[i] = true;

        fs = (((Canvas)this.Content).RenderSize.Width - 30) / features;
        height = ((Canvas)this.Content).RenderSize.Height - 80;
    }
    void ColorInit()
    {
        br[0] = RGB(40, 70, 255); // blue
        br[1] = RGB(255, 188, 0); // gold
        br[2] = RGB(255, 0, 0); // red
        br[3] = RGB(161, 195, 255); // baby blue                  
        br[4] = RGB(0, 255, 0); // green
        br[5] = RGB(255, 0, 255); // magenta
        br[6] = RGB(75, 0, 130); // indigo
        br[7] = RGB(0, 128, 128); // teal
        br[8] = RGB(128, 128, 70); // olive
        br[9] = RGB(255, 222, 0); // yellow
    }

    static DrawingContext ContextHelpMod(bool isInit, ref Canvas cTmp)
    {
        if (!isInit) cTmp.Children.Clear();
        DrawingVisualElement drawingVisual = new();
        cTmp.Children.Add(drawingVisual);
        return drawingVisual.drawingVisual.RenderOpen();
    }
    public static Brush RGB(byte red, byte green, byte blue)
    {
        Brush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }  
} // TheWindow end

public class DrawingVisualElement : FrameworkElement
{
    private readonly VisualCollection _children;
    public DrawingVisual drawingVisual;

    public DrawingVisualElement()
    {
        _children = new VisualCollection(this);
        drawingVisual = new DrawingVisual();
        _children.Add(drawingVisual);
    }

    public void ClearVisualElement()
    {
        _children.Clear();
    }

    protected override int VisualChildrenCount => _children.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }
}

