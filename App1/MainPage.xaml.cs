// Morgan Buford

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechSynthesis;
using Windows.Media.SpeechRecognition;
using Windows.UI.Popups;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace App1
{
    public sealed partial class MainPage : Page
    {
        MediaElement mediaPlayer = new MediaElement();
        SpeechRecognizer recognizer = new SpeechRecognizer();
        double answer;
        double[] numbers = new double[2];

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            RegisterVoiceCommands();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Parameter.ToString()))
                {
                    if (e.NavigationMode == NavigationMode.New)
                    {
                        var result = e.Parameter as SpeechRecognitionResult;
                        var semanticProp = result.SemanticInterpretation.Properties;

                        double operand1 = VoiceToNumber(semanticProp["number1"][0]);
                        double operand2 = VoiceToNumber(semanticProp["number2"][0]);

                        string voiceCommandName = result.RulePath.FirstOrDefault();

                        CalculateExpression(operand1, operand2, semanticProp["commandMode"][0], voiceCommandName);

                        switch (voiceCommandName) // Cortana voice output to use correct grammar
                        {
                            case "plus":
                                ReadText(operand1.ToString() + " plus " + operand2.ToString() + " = " + answer.ToString());
                                break;
                            case "minus":
                                ReadText(operand1.ToString() + " minus " + operand2.ToString() + " = " + answer.ToString());
                                break;
                            case "times":
                                ReadText(operand1.ToString() + " times " + operand2.ToString() + " = " + answer.ToString());
                                break;
                            case "divided by":
                                ReadText(operand1.ToString() + " divided by " + operand2.ToString() + " = " + answer.ToString());
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    await RecognizeSpeech();
                }

            }
            catch (Exception exception)
            {
                const uint HResultPrivacyStatementDeclined = 0x80045509;

                if((uint)exception.HResult == HResultPrivacyStatementDeclined)
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog("You must accept" +
                    "the speech privacy policy to continue.", "Speech Exception");
                    messageDialog.ShowAsync().GetResults();
                }
                else
                {
                    OutputScreen.Text = exception.Message;
                }
            }
        }

        public double VoiceToNumber(string semProp)
        {
            double number = Convert.ToDouble(semProp);
            return number;
        }

        public void CalculateExpression(double num1, double num2, string sProp, string vcName)
        {
            switch(vcName)
            {
                case "plus":
                    answer = num1 + num2;
                    OutputScreen.Text = answer.ToString();
                    break;
                case "minus":
                    answer = num1 - num2;
                    OutputScreen.Text = answer.ToString();
                    break;
                case "times":
                    answer = num1 * num2;
                    OutputScreen.Text = answer.ToString();
                    break;
                case "divided by":
                    if(num2 == 0)
                    {
                        answer = 0;
                        media.Play();
                        ReadText("Congratulations. You just created a black hole.");
                        break;
                    }
                    else answer = num1 / num2;
                    break;
                default:
                    break;
            }
        }

        public void CalculateExpression(double num1, double num2, string vcName)
        {
            switch (vcName)
            {
                case "plus":
                    answer = num1 + num2;
                    ReadText(answer.ToString());
                    break;
                case "minus":
                    answer = num1 - num2;
                    ReadText(answer.ToString());
                    break;
                case "times":
                    answer = num1 * num2;
                    ReadText(answer.ToString());
                    break;
                case "divided by":
                    if(num2 == 0)
                    {
                        answer = 0;
                        media.Play();
                        ReadText("Congratulations. You just created a black hole.");
                        break;
                    }
                    else
                    {
                        answer = num1 / num2;
                        ReadText(answer.ToString());
                    }
                    break;
                default:
                    break;
            }
        }

        // Cortana to speak a text string
        private async void ReadText(string myText)
        {
            using (var speech = new SpeechSynthesizer())
            {
                // Retrieve the first female, US voice
                speech.Voice = SpeechSynthesizer.AllVoices.First(i => (i.Gender == VoiceGender.Female && 
                    i.Description.Contains("United States")));

                // Generate audio stream from plain text
                SpeechSynthesisStream stream = await speech.SynthesizeTextToStreamAsync(myText);

                // Send the stream to the media object
                mediaPlayer.SetSource(stream, stream.ContentType);
                mediaPlayer.Play();
            }
        }

        private async void RegisterVoiceCommands()
        {
            try
            {
                Uri uriVoiceCommands = new Uri("ms-appx:///VoiceCommands.xml", UriKind.Absolute);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uriVoiceCommands);
                await VoiceCommandManager.InstallCommandSetsFromStorageFileAsync(file);
            }
            catch (Exception ex) // Will show if there is an exception
            {
                MessageDialog message = new MessageDialog("Cannot install the VCD file. " + ex.Message);
                message.ShowAsync();
            }
        }

        // In-App Voice Recognition
        private async Task<SpeechRecognitionResult> RecognizeSpeech()
        {
            // One of three Constraint types available
            SpeechRecognitionTopicConstraint topicConstraint
                 = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.WebSearch, "Number");

            recognizer.Constraints.Add(topicConstraint);

            await recognizer.CompileConstraintsAsync(); // Required

            // Put up UI and recognize user's utterance
            SpeechRecognitionResult result = await recognizer.RecognizeWithUIAsync();

            // Check the confidence level of the speech recognition attempt.
            if ((result.Confidence == SpeechRecognitionConfidence.Low) ||
               (result.Confidence == SpeechRecognitionConfidence.Rejected))
            {
                // If the confidence level of the speech recognition attempt is low, 
                // ask the user to try again.
                OutputScreen.Text = "Not sure what you said, please try again.";
                ReadText("Not sure what you said, please try again");
            }
            else
            {
                OutputScreen.Text = result.Text;

                if (result.Text.Contains("plus") ||
                    result.Text.Contains("+"))
                    {
                        extractDigits(result.Text);
                        CalculateExpression(numbers[0], numbers[1], "plus");
                        OutputScreen.Text = answer.ToString(); 
                    }
                else if (result.Text.Contains("minus") ||
                        result.Text.Contains("-"))
                    {
                        extractDigits(result.Text);
                        CalculateExpression(numbers[0], numbers[1], "minus");
                        OutputScreen.Text = answer.ToString(); 
                    }
                else if (result.Text.Contains("times") ||
                        result.Text.Contains("multiplied by") ||
                        result.Text.Contains("*"))
                    {
                        extractDigits(result.Text);
                        CalculateExpression(numbers[0], numbers[1], "times");
                        OutputScreen.Text = answer.ToString(); 
                    }
                else if (result.Text.Contains("divided by") ||
                        result.Text.Contains("/") ||
                        result.Text.Contains("÷"))
                        {
                            extractDigits(result.Text);
                            CalculateExpression(numbers[0], numbers[1], "divided by");
                            OutputScreen.Text = answer.ToString(); 
                        }
                else if (result.Text.Contains("meaning of life"))
                {
                    OutputScreen.Text = "42";
                    ReadText("42");
                }
                else if (result.Text.Contains("Siri"))
                {
                    OutputScreen.Text = "0";
                    ReadText("Cortana rules. Siri drools.");
                }
            }

            return result;
        }

        private void extractDigits(string str)
        {
            string txt = str;
            txt = txt.Replace("O", "0"); // Cortana hears 0 as "o" or "O"
            txt = txt.Replace("o", "0");
            string[] nums = Regex.Split(txt, @"\D+");
            int i = 0;

            foreach (string value in nums)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    numbers[i] = double.Parse(value);
                    OutputScreen.Text = numbers[i].ToString();
                    i++;
                }
            }
        }

        private async void MicButton_Click(object sender, RoutedEventArgs e)
        {
            SpeechRecognitionResult result = await recognizer.RecognizeWithUIAsync();

            // Check the confidence level of the speech recognition attempt.
            if ((result.Confidence == SpeechRecognitionConfidence.Low) ||
               (result.Confidence == SpeechRecognitionConfidence.Rejected))
            {
                // If the confidence level of the speech recognition attempt is low, 
                // ask the user to try again.
                OutputScreen.Text = "Not sure what you said, please try again.";
                ReadText("Not sure what you said, please try again");
            }
            else
            {
                OutputScreen.Text = result.Text;

                if (result.Text.Contains("plus") ||
                    result.Text.Contains("+"))
                {
                    extractDigits(result.Text);
                    CalculateExpression(numbers[0], numbers[1], "plus");
                    OutputScreen.Text = answer.ToString();
                }
                else if (result.Text.Contains("minus") ||
                        result.Text.Contains("-"))
                {
                    extractDigits(result.Text);
                    CalculateExpression(numbers[0], numbers[1], "minus");
                    OutputScreen.Text = answer.ToString();
                }
                else if (result.Text.Contains("times") ||
                        result.Text.Contains("multiplied by") ||
                        result.Text.Contains("*"))
                {
                    extractDigits(result.Text);
                    CalculateExpression(numbers[0], numbers[1], "times");
                    OutputScreen.Text = answer.ToString();
                }
                else if (result.Text.Contains("divided by") ||
                        result.Text.Contains("/") ||
                        result.Text.Contains("÷"))
                {
                    extractDigits(result.Text);
                    CalculateExpression(numbers[0], numbers[1], "divided by");
                    OutputScreen.Text = answer.ToString();
                }
                else if (result.Text.Contains("meaning of life"))
                {
                    OutputScreen.Text = "42";
                    ReadText("42");
                }
                else if (result.Text.Contains("Siri"))
                {
                    OutputScreen.Text = "0";
                    ReadText("Cortana rules. Siri drools.");
                }
            }

        }
    }
}
