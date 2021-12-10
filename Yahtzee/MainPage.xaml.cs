using System;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Yahtzee
{
    public partial class MainPage : ContentPage
    {
        // Used because android doesnt allow numbers in the png files
        private static readonly string[] numbersToText = new string[] { "one", "two", "three", "four", "five", "six" };

        // Consts
        private const int NumberOfDiceFaces = 6;
        private const int NumberOfDice = 5;
        private const int NumberOfScores = 13;
        private const int NumberOfLowerScores = 6;

        // Score index
        // 0-5 aces to sixes
        private const int ThreeOfAKindIndex = 6;
        private const int FourOfAKindIndex = 7;
        private const int FullHouseIndex = 8;
        private const int SmallStraightIndex = 9;
        private const int LargeStraightIndex = 10;
        private const int ChanceIndex = 11;
        private const int YahtzeeIndex = 12;


        // Dice Visuals
        private readonly ImageButton[] diceButtonVisuals = new ImageButton[NumberOfDice];
        private bool[] diceHeld;
        private int[] diceValues;

        // Score Visuals
        private readonly Button[] scoreVisuals = new Button[NumberOfScores];
        private bool[] lockedScores;
        private int[] scoreValues;

        // Member fields
        private Random rng;

        private int rollsLeft;
        private bool rollingDice;

        public MainPage()
        {
            InitializeComponent();
        }

        // Wait for page to load then display dice images
        protected override void OnAppearing()
        {
            rng = new Random();

            for (int i = 0; i < NumberOfDice; i++)
            {
                diceButtonVisuals[i] = diceGrid.FindByName<ImageButton>("dice" + i);
            }

            for (int i = 0; i < NumberOfScores; i++)
            {
                scoreVisuals[i] = scoreGrid.FindByName<Button>("score" + i);
            }

            RestartGame();

            UpdateDiceVisuals();
            UpdateScoreVisuals();
        }

        #region Game Functions

        void RestartGame()
        {
            rollsLeft = 3;

            diceValues = new int[NumberOfDice];
            diceHeld = new bool[NumberOfDice];
            lockedScores = new bool[NumberOfScores];
            scoreValues = new int[NumberOfScores];

            lowerTotal.Text = "0";
            upperTotal.Text = "0";

            NextTurn();
        }

        /// <summary>
        /// Rolls all the dice not held
        /// </summary>
        private async void RollDice()
        {
            rollsLeft--;
            if (rollsLeft <= 0)
            {
                rollButton.IsEnabled = false;
            }

            // Display Plural when needed
            if (rollsLeft == 1)
            {
                rollButton.Text = $"1 Roll Left";
            }
            else
            {
                rollButton.Text = $"{rollsLeft} Rolls Left";
            }


            // The real randomization roll
            for (int j = 0; j < NumberOfDice; j++)
            {
                if (diceHeld[j] == false)
                {
                    //diceValues[j] = 5;
                    diceValues[j] = rng.Next(6);
                }
            }

            rollingDice = true;

            const int RollAnimationCount = 4;

            // Do fancy animation
            for (int i = 0; i < RollAnimationCount; i++)
            {
                for (int j = 0; j < NumberOfDice; j++)
                {
                    if (diceHeld[j] == false)
                    {
                        diceButtonVisuals[j].Source = "dice_" + numbersToText[rng.Next(6)] + ".png";

                        await Task.Delay(25);
                    }
                }
                await Task.Delay(50);
            }

            rollingDice = false;

            UpdateDiceVisuals();
            UpdateScores();
        }

        /// <summary>
        /// Move on to the next round
        /// </summary>
        private void NextTurn()
        {
            for (int i = 0; i < NumberOfDice; i++)
            {
                diceHeld[i] = false;
            }

            rollsLeft = 3;

            rollButton.IsEnabled = true;
            rollButton.Text = $"Roll ({rollsLeft})";

            UpdateDiceVisuals();
            UpdateScores();

            RollDice();
        }


        /// <summary>
        /// Update the score from the dice values
        /// </summary>
        private void UpdateScores()
        {
            // Zero out the scores
            for (int i = 0; i < scoreValues.Length; i++)
            {
                if (!lockedScores[i])
                {
                    scoreValues[i] = 0;
                }
            }

            // count of each number on dice
            int[] diceFaceCount = new int[NumberOfDiceFaces];

            for (int i = 0; i < diceValues.Length; i++)
            {
                int diceValue = diceValues[i];
                diceFaceCount[diceValue]++;
            }

            CalculateScores(diceFaceCount);

            UpdateScoreVisuals();
        }

        /// <summary>
        /// Calculate what scores are available for the current roll
        /// </summary>
        /// <param name="diceFaceCount">The current roll</param>
        private void CalculateScores(int[] diceFaceCount)
        {
            int upperScoreTotal = 0;
            int totalDiceValue = 0;

            for (int i = 0; i < diceFaceCount.Length; i++)
            {
                int diceValue = diceFaceCount[i] * (i + 1);

                // Aces to sixes
                if (!lockedScores[i])
                {
                    scoreValues[i] = diceValue;
                }

                if (lockedScores[i])
                {
                    upperScoreTotal += scoreValues[i];
                }

                totalDiceValue += diceValue;
            }

            const int BonusScore = 35;
            if (upperScoreTotal >= 63)
            {
                bonusScore.Text = BonusScore.ToString();
            }

            bool fullhouse3Common = false;
            bool fullhouse2Common = false;

            for (int i = 0; i < NumberOfDiceFaces; i++)
            {
                // Three of a kind
                if (diceFaceCount[i] >= 3 && !lockedScores[ThreeOfAKindIndex])
                {
                    scoreValues[ThreeOfAKindIndex] = totalDiceValue;
                }

                // Four of a kind
                if (diceFaceCount[i] >= 4 && !lockedScores[FourOfAKindIndex])
                {
                    scoreValues[FourOfAKindIndex] = totalDiceValue;
                }

                // Full House
                if (diceFaceCount[i] == 3)
                {
                    fullhouse3Common = true;
                }

                if (diceFaceCount[i] == 2)
                {
                    fullhouse2Common = true;
                }
            }

            const int FullHouseScore = 25;
            if (fullhouse3Common && fullhouse2Common && !lockedScores[FullHouseIndex])
            {
                scoreValues[FullHouseIndex] = FullHouseScore;
            }

            // Small Straight
            const int SmallStraightScore = 30;
            for (int i = 0; i < 3; i++)
            {
                if (diceFaceCount[i] >= 1 && diceFaceCount[i + 1] >= 1 && diceFaceCount[i + 2] >= 1 && diceFaceCount[i + 3] >= 1)
                {
                    if (!lockedScores[SmallStraightIndex])
                    {
                        scoreValues[SmallStraightIndex] = SmallStraightScore;
                    }
                }
            }

            // Large Straight
            const int LargeStraight = 40;
            for (int i = 0; i < 2; i++)
            {
                if (diceFaceCount[i] == 1 && diceFaceCount[i + 1] == 1 && diceFaceCount[i + 2] == 1 && diceFaceCount[i + 3] == 1 && diceFaceCount[i + 4] == 1)
                {
                    if (!lockedScores[LargeStraightIndex])
                    {
                        scoreValues[LargeStraightIndex] = LargeStraight;
                    }
                }
            }

            // Chance
            if (!lockedScores[ChanceIndex])
            {
                scoreValues[ChanceIndex] = totalDiceValue;
            }

            // Yahtzee
            const int YahtzeeScore = 50;
            const int YahtzeeBonusScore = 100;
            if (diceFaceCount[0] == 5 || diceFaceCount[1] == 5 || diceFaceCount[2] == 5 || diceFaceCount[3] == 5 || diceFaceCount[4] == 5 || diceFaceCount[5] == 5)
            {
                if (scoreValues[YahtzeeIndex] >= YahtzeeScore)
                {
                    // Cant have a yatzee bonus if locked at zero
                    if (lockedScores[YahtzeeIndex] && scoreValues[YahtzeeIndex] == YahtzeeScore)
                    {
                        scoreValues[YahtzeeIndex] += YahtzeeBonusScore;
                    }
                }
                else
                {
                    if (!lockedScores[YahtzeeIndex])
                    {
                        scoreValues[YahtzeeIndex] = YahtzeeScore;
                    }
                }
            }

        }

        #endregion

        #region Update Visuals

        /// <summary>
        /// Update the score buttons
        /// </summary>
        private void UpdateScoreVisuals()
        {
            // Disable the buttons or update the number on it
            for (int i = 0; i < NumberOfScores; i++)
            {
                scoreVisuals[i].Text = scoreValues[i].ToString();

                if (lockedScores[i])
                {
                    scoreVisuals[i].BackgroundColor = Color.Transparent;
                    scoreVisuals[i].TextColor = Color.FromHex("FFCA2222");
                }
                else
                {
                    scoreVisuals[i].BackgroundColor = Color.FromHex("FFCA2222");
                    scoreVisuals[i].TextColor = Color.FromHex("FFEA8D23");
                }
            }
        }

        /// <summary>
        /// Update the dice image buttons
        /// </summary>
        private void UpdateDiceVisuals()
        {
            for (int i = 0; i < NumberOfDice; i++)
            {
                int roll = diceValues[i];

                diceButtonVisuals[i].Source = "dice_" + numbersToText[roll] + ".png";

                if (diceHeld[i])
                {
                    diceButtonVisuals[i].TranslateTo(0, 20, 100);
                }
                else
                {
                    diceButtonVisuals[i].TranslateTo(0, 0, 100);
                }
            }
        }

        #endregion

        #region Clicked Events

        /// <summary>
        /// Roll dice button on click
        /// </summary>
        private void RollDice_Clicked(object sender, EventArgs e)
        {
            RollDice();
        }


        /// <summary>
        /// Toggles holding the dice
        /// </summary>
        private void HoldDice_Clicked(object sender, EventArgs e)
        {
            ImageButton diceButton = (ImageButton)sender;

            int diceIndex = (int)diceButton.GetValue(Grid.ColumnProperty);
            diceHeld[diceIndex] = !diceHeld[diceIndex];

            UpdateDiceVisuals();
        }

        /// <summary>
        /// Lock the score you selected
        /// </summary>
        private async void LockScore_Clicked(object sender, EventArgs e)
        {
            if (rollingDice)
            {
                return;
            }

            Button button = (Button)sender;

            // Get the index of the score we clicked on
            int index = Convert.ToInt32(button.StyleId);

            int highestScore = -1;
            for (int i = 0; i < scoreVisuals.Length; i++)
            {
                // Ignore chance as it will often be bigger
                if (scoreValues[i] > highestScore && lockedScores[i] == false && i != ChanceIndex)
                {
                    highestScore = scoreValues[i];
                }
            }

            // check if selected a smaller score
            if (scoreValues[index] < highestScore)
            {
                bool t = await DisplayAlert($"Are you sure you want {scoreValues[index]}?", $"There is a higher score available at {highestScore}.", "Yes", "No");
                if (t == false)
                {
                    return;
                }
            }

            lockedScores[index] = true;

            int upperScore = 0;
            int lowerScore = 0;
            bool gameover = true;
            for (int i = 0; i < scoreValues.Length; i++)
            {
                if (lockedScores[i])
                {
                    if (i >= NumberOfLowerScores)
                    {
                        lowerScore += scoreValues[i];
                    }
                    else
                    {
                        upperScore += scoreValues[i];
                    }
                }

                if (lockedScores[i] == false)
                {
                    gameover = false;
                }
            }

            NextTurn();

            if (bonusScore.Text != "0")
            {
                upperScore += 35;
            }

            lowerTotal.Text = lowerScore + "";
            upperTotal.Text = upperScore + "";

            if (gameover)
            {
                await DisplayAlert("", $"Upper score: {upperScore}\nLower score: {lowerScore}\n\nTotal score: {upperScore + lowerScore}", "New Game");
                RestartGame();
            }
        }

        /// <summary>
        /// New Game Button Clicked
        /// </summary>
        private async void NewGame_Clicked(object sender, EventArgs e)
        {
            bool newGame = await DisplayAlert("New Game", "Are you sure you want to start a new game?", "New Game", "Cancel");
            if (newGame)
            {
                RestartGame();
            }
        }

        #endregion

    }
}
