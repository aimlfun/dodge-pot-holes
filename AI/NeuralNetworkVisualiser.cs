using CarDodge.Utilities;
using System.Drawing.Drawing2D;

namespace CarDodge.AI;

/// <summary>
/// Draw a visual representation of the neural network.
/// </summary>
internal static class NeuralNetworkVisualiser
{

    /// <summary>
    /// Label for neurons.
    /// </summary>
    internal readonly static Font fontForNeuronLabel = new("Arial", 7);

    /// <summary>
    /// We draw a circle around the neurons, because otherwise they look missing when the neuron isn't firing.
    /// </summary>
    private readonly static Pen s_penNeuronOutline = new(Color.FromArgb(180, 255, 255, 255));

    /// <summary>
    /// Last car's network drawn to image (lines are unique to the car's network).
    /// This enables us to determine the need to repaint the lines image.
    /// </summary>
    private static int s_idLastDrawn = -1;

    /// <summary>
    /// Background image that doesn't change each time the car moves.
    /// </summary>
    private static Bitmap? s_imageOfStaticPartsForThisNetworkThatDontNeedPaintingEachTime = null;

    /// <summary>
    /// Max neuron diameter.
    /// </summary>
    internal const int s_maxDiameter = 25;

    /// <summary>
    /// Used to space out the neurons
    /// </summary>
    private static int s_maxlen;

    /// <summary>
    /// Computes where neuron should appear left-to-right.
    /// </summary>
    /// <param name="maxlen"></param>
    /// <param name="neuronsInLayer"></param>
    /// <returns></returns>
    private static int OffsetForNeuronBlock(int maxlen, int neuronsInLayer)
    {
        return (maxlen - neuronsInLayer * (s_maxDiameter + 2)) / 2 - s_maxDiameter;
    }

    /// <summary>
    /// Draw weighted lines between neurons. Semantic colouring and thickness.
    /// Alpha is increased and line is thicker the higher the weighting.
    /// Those with a negative weighting we draw red, and positive green.
    /// </summary>
    /// <param name="network"></param>
    /// <param name="graphicsOfVisualiser"></param>
    /// <param name="nnY"></param>
    /// <param name="maxlen"></param>
    private static void DrawNeuralNetworkLines(NeuralNetwork network, Graphics graphicsOfVisualiser, int nnY, out int maxlen)
    {
        maxlen = 0;

        for (int layer = 0; layer < network.Layers.Length; layer++)
        {
            int length = network.Neurons[layer].Length * (s_maxDiameter + 2) + 5 + s_maxDiameter + 5;

            if (length > maxlen) maxlen = length;
        }

        // maxlen is half. 
        int diff = 350;

        maxlen += diff + s_maxDiameter + 10;

        for (int layer = 1; layer < network.Layers.Length; layer++)
        {
            for (int j = 0; j < network.Neurons[layer].Length; j++)
            {
                int offsetJ = OffsetForNeuronBlock(maxlen, network.Neurons[layer].Length);

                for (int k = 0; k < network.Neurons[layer - 1].Length; k++)
                {
                    int offsetK = OffsetForNeuronBlock(maxlen, network.Neurons[layer - 1].Length);

                    int posXThisLayer = j * (s_maxDiameter + 2) + 10 + s_maxDiameter + offsetJ;
                    int posXPriorLayer = k * (s_maxDiameter + 2) + 10 + s_maxDiameter + offsetK;

                    int posYThisLayer = nnY + layer * (s_maxDiameter + 20);
                    int posYPriorLayer = nnY + (layer - 1) * (s_maxDiameter + 20);

                    double rawWeight = network.Weights[layer - 1][j][k];

                    double weight = Math.Max(Math.Abs(rawWeight), 0.5F);

                    int alpha = Math.Min(255, Math.Max((int)(Math.Abs(rawWeight) * 256), 1));

                    graphicsOfVisualiser.DrawLine(
                            new Pen(Color.FromArgb(alpha, // based on weight
                                                   rawWeight < 0 ? 255 : 0, // <0 ==> red 
                                                    rawWeight > 0 ? 148 : 0, // >0 ==> green
                                                    rawWeight == 0 ? 255 : 0), // ==> blue 
                                                    (int)(weight * 8)), posXPriorLayer - 184, posYPriorLayer + 200, posXThisLayer - 184, posYThisLayer + 200);
                }
            }
        }
    }

    /// <summary>
    /// Draws "blobs" per neuron, indicating activity in it via brightness.
    /// </summary>
    /// <param name="network"></param>
    /// <param name="graphicsOfVisualiser"></param>
    /// <param name="nnY"></param>
    /// <param name="maxlen"></param>
    private static void DrawNeuralNetworkActivity(NeuralNetwork network, Graphics graphicsOfVisualiser, int nnY, int maxlen)
    {
        for (int layer = 0; layer < network.Layers.Length; layer++)
        {
            int len = network.Neurons[layer].Length;

            for (int j = 0; j < len; j++)
            {
                int offsetJ = OffsetForNeuronBlock(maxlen, len);

                double output = network.Neurons[layer][j];
                float sizeBasedOnBIAS = (float)(network.Biases[layer][j] + 0.5F);

                int sizeOfBlob = (int)Math.Round(sizeBasedOnBIAS * s_maxDiameter * 1);

                int posXThisLayer = j * (s_maxDiameter + 2) + 10 + s_maxDiameter + offsetJ;
                int posYThisLayer = nnY + layer * (s_maxDiameter + 20);

                // input has values at top of it, output has values beneath,
                if (layer == 0 || layer == network.Layers.Length - 1)
                {
                    graphicsOfVisualiser.DrawString(Math.Round(network.Neurons[layer][j], 2).ToString(),
                        fontForNeuronLabel,
                        Brushes.White,
                        new PointF(posXThisLayer - 10 - 184, 200 +
                        (layer == 0 ? nnY - s_maxDiameter + 3 : posYThisLayer + s_maxDiameter)));
                }

                using SolidBrush brushBlob = new(Color.FromArgb(255, output < 0 ? 255 : 0, output > 0 ? 148 : 0, output == 0 ? 120 : 0));
                sizeOfBlob = (int)(Math.Abs(output) * sizeOfBlob);
                sizeOfBlob = sizeOfBlob / 2 * 2 + 1;
                graphicsOfVisualiser.FillEllipse(brushBlob, posXThisLayer - (float)sizeOfBlob / 2 - 183.5F,
                                                            posYThisLayer - (float)sizeOfBlob / 2 + 200.5F,
                                                            sizeOfBlob, sizeOfBlob);
            }
        }
    }

    /// <summary>
    /// Draws circles around the neurons (size based on biases).
    /// </summary>
    /// <param name="network"></param>
    /// <param name="graphicsOfVisualiser"></param>
    /// <param name="nnY"></param>
    /// <param name="maxlen"></param>
    private static void DrawNeuralNetworkActivityCircles(NeuralNetwork network, Graphics graphicsOfVisualiser, int nnY, int maxlen)
    {
        for (int layer = 0; layer < network.Layers.Length; layer++)
        {
            for (int neuronIndex = 0; neuronIndex < network.Neurons[layer].Length; neuronIndex++)
            {
                int offsetJ = OffsetForNeuronBlock(maxlen, network.Neurons[layer].Length);

                float sizeBasedOnBIAS = (float)(network.Biases[layer][neuronIndex] + 0.5F);

                int sizeOfBlob = (int)Math.Round(sizeBasedOnBIAS * s_maxDiameter * 1);

                int posXThisLayer = neuronIndex * (s_maxDiameter + 2) + 10 + s_maxDiameter + offsetJ;
                int posYThisLayer = nnY + layer * (s_maxDiameter + 20);

                graphicsOfVisualiser.DrawEllipse(s_penNeuronOutline, posXThisLayer - sizeOfBlob / 2 - 184,
                                                                   posYThisLayer - sizeOfBlob / 2 + 200,
                                                                   sizeOfBlob, sizeOfBlob);
            }
        }
    }

    /// <summary>
    /// Resets the visualiser (so it re-draws next time).
    /// </summary>
    internal static void Reset()
    {
        s_idLastDrawn = -int.MaxValue;
        s_imageOfStaticPartsForThisNetworkThatDontNeedPaintingEachTime = null;
    }

    /// <summary>
    /// Draws the neurons in real-time: that means painting the static parts the first time for this "id", and overlaying the adctivity.
    /// </summary>
    /// <param name="network"></param>
    /// <param name="visualImage"></param>
    /// <param name="graphicsOfVisualiser"></param>
    /// <param name="nnY"></param>
    /// <returns></returns>
    internal static void DrawNeuralFiringAsColouredBlobs(NeuralNetwork network, Bitmap visualImage, Graphics graphicsOfVisualiser, int nnY)
    {
        // OPTIMISATION: if the network we are drawing is the same one, there are static parts that do not need redrawing.
        // This is logic to create that static imagery. We put it into a Bitmap and blit it on underneath neurons firing.
        if (s_idLastDrawn != network.Id || s_imageOfStaticPartsForThisNetworkThatDontNeedPaintingEachTime == null)
        {
            DrawStaticPartsOfDashboard(network, visualImage, nnY);
        }

        // add the background static imagery stuff (the bits that don't change).
#pragma warning disable CS8604 // Possible null reference argument. DrawStaticPartsOfDashboard() populates s_imageOfStaticPartsForThisNetworkThatDontNeedPaintingEachTime, ignore warning
        graphicsOfVisualiser.DrawImageUnscaled(s_imageOfStaticPartsForThisNetworkThatDontNeedPaintingEachTime, 0, 0);
#pragma warning restore CS8604 // Possible null reference argument.

        // overlay firing neurons
        DrawNeuralNetworkActivity(network, graphicsOfVisualiser, nnY, s_maxlen);
    }

    /// <summary>
    /// For performance reasons, we pre-draw the parts that do not change each time it refreshes.
    /// </summary>
    /// <param name="network"></param>
    /// <param name="visualImage"></param>
    /// <param name="nnY"></param>
    /// <returns></returns>
    private static void DrawStaticPartsOfDashboard(NeuralNetwork network, Bitmap visualImage, int nnY)
    {
        // track the network, and make a blank image.
        s_idLastDrawn = network.Id;
        s_imageOfStaticPartsForThisNetworkThatDontNeedPaintingEachTime = new Bitmap(visualImage.Width, visualImage.Height);
        using Graphics g = Graphics.FromImage(s_imageOfStaticPartsForThisNetworkThatDontNeedPaintingEachTime);

        // this dashboard image is ruined if the graphics are blocky, so we set high quality at expense of performance
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;

        // these are the lines between neurons
        DrawNeuralNetworkLines(network, g, nnY, out s_maxlen);

        // draw circles around the neuron
        DrawNeuralNetworkActivityCircles(network, g, nnY, s_maxlen);
    }

    /// <summary>
    /// Really cool visualisation in real-time.
    /// </summary>
    /// <returns></returns>
    internal static void Render(NeuralNetwork network, PictureBox canvas)
    {
        if (network.Id != s_idLastDrawn) Reset();

        // sized big enough for circle map + neural network (that varies in width based on the largest neuron layer)
        Bitmap visualImage = new(canvas.Width, canvas.Height);

        using Graphics graphicsOfVisualImage = Graphics.FromImage(visualImage);
        graphicsOfVisualImage.Clear(Color.Black);

        // this dashboard image is ruined if the graphics are blocky, so we set high quality at expense of performance
        graphicsOfVisualImage.ToHighQuality();

        DrawNeuralFiringAsColouredBlobs(network, visualImage, graphicsOfVisualImage, -170);

        // provide the image for "paint" operation
        canvas.Image?.Dispose();
        canvas.Image = visualImage;
    }
}