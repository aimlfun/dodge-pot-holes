using CarDodge.AI;
using CarDodge.Settings;
using CarDodge.Utilities;
using System.Text;

namespace CarDodge
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Contains the list of pot holes to move/draw (on-screen).
        /// </summary>
        readonly List<Point> listOfPotHoles = new();

        /// <summary>
        /// This is how much of road can be seen.
        /// </summary>
        internal static int visibleRoadLengthPX = 788;

        /// <summary>
        /// This is the width of the road.
        /// </summary>
        internal static int roadWidthPX = 117;

        /// <summary>
        /// This is the left most viewable point on the viewport.
        /// </summary>
        internal int viewPortLeftEdgeX = 0;

        /// <summary>
        /// The minimum distance potholes must be apart.
        /// </summary>
        private readonly int potHoleThreshold = 60;

        /// <summary>
        /// Used to determine when to add a pothole.
        /// </summary>
        private int nextPotHoleTimer;

        /// <summary>
        /// Width of each lane in pixels.
        /// </summary>
        readonly float laneWidthPX;

        /// <summary>
        /// Pseudo random number generator.
        /// </summary>
        readonly Random pseudoRandomNumberGenerator = new(111);

        /// <summary>
        /// Constructor.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // make the car background transparent, and sized for the lanes.
            AICar.s_carBitmap.MakeTransparent(AICar.s_carBitmap.GetPixel(0, 0));
            AICar.s_carBitmap = (Bitmap)Utils.ResizeImage((Image)AICar.s_carBitmap, AICar.s_potholeWidth + 20, AICar.s_potholeWidth + 20);
            AICar.s_potholeHeight = roadWidthPX / 4 - 20;

            laneWidthPX = roadWidthPX / 4;

            InitialiseTheNeuralNetworksForTheCars();
            CreateNewGeneration();

            timer1.Enabled = true;
            timer1.Start();
        }

        /// <summary>
        /// Initialises the neural network (one per car).
        /// </summary>
        internal static void InitialiseTheNeuralNetworksForTheCars()
        {
            ConfigAI aiConfig = Settings.Config.s_settings.AI;

            NeuralNetwork.s_networks.Clear();

            for (int i = 0; i < Config.s_settings.AI.NumberOfAICarsToCreate; i++)
            {
                _ = new NeuralNetwork(i, new int[] { aiConfig.SamplePoints, 2 });
            }
        }

        /// <summary>
        /// Creates a generation (of cars).
        /// </summary>
        private void CreateNewGeneration()
        {
            viewPortLeftEdgeX = 30; // reset viewport left edge.

            AICar.NewGeneration(visibleRoadLengthPX, (int)laneWidthPX);
        }

        /// <summary>
        /// Draws the road, the AI cars and overlays potholes.
        /// </summary>
        private bool DrawRoadCarAndPotholes()
        {
            MovePotHoles();

            Bitmap road = new(788, 103);

            using Graphics g = Graphics.FromImage(road);
            g.Clear(Color.FromArgb(255, 120, 120, 122)); // tarmac colour

            DrawLanes(road, g);

            DrawPotHoles(g);

            g.Flush();

            bool hit = AICar.MoveAICars(road, viewPortLeftEdgeX, out float maxCarX, out int idRemainingCar);

            AICar.DrawCars(g, viewPortLeftEdgeX);

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = road;

            // move the viewport as necessary for the car furthest along the road.
            if (maxCarX > viewPortLeftEdgeX + visibleRoadLengthPX / 2) viewPortLeftEdgeX += (int)(maxCarX - (viewPortLeftEdgeX + visibleRoadLengthPX / 2));

            // last car gets neural network displayed.
            if (idRemainingCar != -1) Visualise(idRemainingCar); else pictureBoxNeuralNetworkVisualiser.Visible = false;

            return hit;
        }

        /// <summary>
        /// Shows the "brains" in action when there is just one car.
        /// </summary>
        /// <param name="id"></param>
        private void Visualise(int id)
        {
            pictureBoxNeuralNetworkVisualiser.Image?.Dispose();
            NeuralNetworkVisualiser.Render(NeuralNetwork.s_networks[id], pictureBoxNeuralNetworkVisualiser);
            pictureBoxNeuralNetworkVisualiser.Visible = true;
        }

        /// <summary>
        /// Draws the lanes with dotted lines, and red edge top/bottom.
        /// </summary>
        /// <param name="road"></param>
        /// <param name="g"></param>
        private void DrawLanes(Bitmap road, Graphics g)
        {
            int offsetX = -(viewPortLeftEdgeX % 9);

            g.DrawLine(Config.s_settings.Display.RedOutOfBoundsLines, 0, 1, road.Width, 1); // top edge = red

            g.DrawLine(Pens.White, offsetX, 2, road.Width, 2);
            g.DrawLine(Config.s_settings.Display.DottedRoadLaneLinesPen, offsetX, roadWidthPX / 2, visibleRoadLengthPX, roadWidthPX / 2);
            g.DrawLine(Config.s_settings.Display.DottedRoadLaneLinesPen, offsetX, roadWidthPX / 4, visibleRoadLengthPX, roadWidthPX / 4);
            g.DrawLine(Pens.White, offsetX, roadWidthPX / 4 * 3, visibleRoadLengthPX, roadWidthPX / 4 * 3);

            g.DrawLine(Config.s_settings.Display.RedOutOfBoundsLines, 0, roadWidthPX / 4 * 3 + 2, road.Width, roadWidthPX / 4 * 3 + 2); // bottom edge red
            g.DrawString((viewPortLeftEdgeX + visibleRoadLengthPX / 2).ToString(), new Font("Arial", 7), Brushes.White, visibleRoadLengthPX / 2 - 20, roadWidthPX / 4 * 3 + 2);
        }

        /// <summary>
        /// Draw a pot holes - 2 ellipses, one with red edge, used to check for impact with pothole.
        /// </summary>
        /// <param name="g"></param>
        private void DrawPotHoles(Graphics g)
        {
            // draw the visible potholes.
            foreach (Point point in listOfPotHoles)
            {
                g.FillEllipse(Brushes.Red, new Rectangle(point.X - AICar.s_potholeWidth / 2 - 4 - viewPortLeftEdgeX, point.Y - 16 / 2, AICar.s_potholeWidth - 8, 16));
                g.FillEllipse(Brushes.Black, new Rectangle(point.X - AICar.s_potholeWidth / 2 - 4 - viewPortLeftEdgeX+2, point.Y - 16 / 2+1, AICar.s_potholeWidth - 8-2, 16-2));
            }
        }

        /// <summary>
        /// Animation: adds a pot hole if necessary, then draws road and car.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            // ready to add a pot hole?
            if (listOfPotHoles.Count <= potHoleThreshold)
            {
                AddPotHole();
            }

            if (DrawRoadCarAndPotholes()) // true-> all cars crashed, time for next generation
            {
                NextGeneration();
            }
        }

        /// <summary>
        /// Stops the timer, mutates the AI, creates new cars, resets the road.
        /// </summary>
        private void NextGeneration(bool dontMutate = false)
        {
            timer1.Stop();

            if (!dontMutate) MutateCars();
            
            listOfPotHoles.Clear();
            nextPotHoleTimer = 0;
            
            CreateNewGeneration();
            
            timer1.Start();
        }

        /// <summary>
        /// Mutates bottom performing 50% of cars, copying the top 50% and mutating.
        /// </summary>
        private static void MutateCars()
        {
            NeuralNetwork.SortNetworkByFitness(); // largest "fitness" (best performing) goes to the bottom

            // sorting is great but index no longer matches the "id".
            // this is because the sort swaps but this misaligns id with the entry            
            List<NeuralNetwork> n = new();
            foreach (int n2 in NeuralNetwork.s_networks.Keys) n.Add(NeuralNetwork.s_networks[n2]);

            NeuralNetwork[] array = n.ToArray();

            // replace the 50% worse offenders with the best, then mutate them.
            // we do this by copying top half (lowest fitness) with top half.
            for (int worstNeuralNetworkIndex = 0; worstNeuralNetworkIndex < Config.s_settings.AI.NumberOfAICarsToCreate / 2; worstNeuralNetworkIndex++)
            {
                // 50..100 (in 100 neural networks) are in the top performing
                int neuralNetworkToCloneFromIndex = worstNeuralNetworkIndex + Config.s_settings.AI.NumberOfAICarsToCreate / 2; // +50% -> top 50% 

                NeuralNetwork.CopyFromTo(array[neuralNetworkToCloneFromIndex], array[worstNeuralNetworkIndex]); // copy

                array[worstNeuralNetworkIndex].Mutate(25, 0.5F); // mutate
                array[worstNeuralNetworkIndex].Fitness = -array[neuralNetworkToCloneFromIndex].Fitness;
            }

            // unsort, restoring the order of car to neural network i.e [x]=id of "x".
            Dictionary<int, NeuralNetwork> unsortedNetworksDictionary = new();

            for (int carIndex = 0; carIndex < Config.s_settings.AI.NumberOfAICarsToCreate; carIndex++)
            {
                var neuralNetwork = NeuralNetwork.s_networks[carIndex];

                unsortedNetworksDictionary[neuralNetwork.Id] = neuralNetwork;
            }

            NeuralNetwork.s_networks = unsortedNetworksDictionary;
        }

        /// <summary>
        /// Moves the potholes left, and removes them from the list if they scroll off screen.
        /// </summary>
        private void MovePotHoles()
        {
            Point[] p = listOfPotHoles.ToArray();

            List<int> toRemove = new();

            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].X < viewPortLeftEdgeX) toRemove.Add(i);
            }

            // these are off screen, remove.
            while (toRemove.Count > 0)
            {
                listOfPotHoles.RemoveAt(toRemove[^1]);
                toRemove.RemoveAt(toRemove.Count - 1);
            }
        }

        /// <summary>
        /// Adds a new pot hole.
        /// </summary>
        private void AddPotHole()
        {
            if (nextPotHoleTimer > viewPortLeftEdgeX + visibleRoadLengthPX) return;
            {
                int posx = AICar.s_potholeWidth + visibleRoadLengthPX + viewPortLeftEdgeX;
                int lane = pseudoRandomNumberGenerator.Next(3);

                listOfPotHoles.Add(new Point(posx, (int)((1F + (float)lane) * laneWidthPX - laneWidthPX / 2)));

                // set timer to ensure we don't put them too close together.
                nextPotHoleTimer = (int)((float)posx + AICar.s_potholeWidth * 1F + pseudoRandomNumberGenerator.Next(5) - 3);
            }
        }

        /// <summary>
        /// User can press keys to save/load model, pause/slow, mutate etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.P:
                    // "P" pauses the timer (and what's happening)
                    timer1.Enabled = !timer1.Enabled;
                    if (timer1.Enabled) timer1.Start(); else timer1.Stop();
                    break;

                case Keys.S:
                    // CTRL-S saves the model | "S" slow mode
                    if (e.Control)
                        NeuralNetwork.SaveTrainedModel();
                    else
                        StepThroughSpeeds();
                    break;

                case Keys.L:
                    // CTRL-L loads the model | "L" shows lidar
                    if (e.Control)
                    {
                        NeuralNetwork.LoadTrainedModel();
                        NextGeneration(dontMutate: true);
                    }
                    else
                    {
                        Config.s_settings.Display.ShowLIDAR = !Config.s_settings.Display.ShowLIDAR;
                    }
                    break;

                case Keys.H:
                    // show hit points
                    Config.s_settings.Display.ShowHitPointsOnCar = !Config.s_settings.Display.ShowHitPointsOnCar;
                    break;

                case Keys.M:
                    // mutate
                    NextGeneration();
                    break;
            }
        }

        /// <summary>
        /// S slows things down, 2x slower, 5x slower, 10x slower, then back to normal speed.
        /// </summary>
        private void StepThroughSpeeds()
        {
            var newInterval = timer1.Interval switch
            {
                10 => 20,
                20 => 50,
                50 => 100,
                _ => 10,
            };

            timer1.Interval = newInterval;
        }

    }
}