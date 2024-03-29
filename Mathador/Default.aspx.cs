﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using MySql.Data.MySqlClient;
using System.Web.UI.WebControls;
using ControllerLib;
using MoteurLib;
using System.IO;

namespace Mathador
{

    public partial class _Default : Page
    {
        private Stack<String> pile = new Stack<String>();
        private Controller controller = new Controller();
        private Moteur moteur = new Moteur();
        public List<KeyValuePair<int, List<String>>> values = new List<KeyValuePair<int, List<String>>>();
        public List<KeyValuePair<String, int>> highScores = new List<KeyValuePair<String, int>>();

        protected void Page_Load(object sender, EventArgs e)
        {
            //Initialise Cache and DOM
            highScores = controller.getHighScores();
            setCache();
            setButtons();
        }

        protected void ajouterPile(object sender, EventArgs e)
        {
            Button senderButton = (Button)sender;
            TimeSpan time = new TimeSpan();
            time = (DateTime)Cache["time"] - DateTime.Now;

            //Check Time Left
            if (time.Seconds <= 0 && time.Minutes <= 0)
            {
                Errors.Text = "Le temps est écoulé ! Veuillez renseigner votre pseudo !";
                return;
            }
            try
            {
                //Check if the value can be used
                pile = (Stack<String>)Cache["pile"];
                if (senderButton.Text.Equals("+") || senderButton.Text.Equals("-") || senderButton.Text.Equals("*") || senderButton.Text.Equals("/"))
                {
                    if (pile.Count == 1)
                    {
                        pile.Push(senderButton.Text);
                    }
                    else
                    {
                        pile.Clear();
                        Cache.Remove("pile");
                        Cache.Remove("lastButtonID");
                        Errors.Text = "Opération non valable : l'opérateur doit être en deuxième position";
                    }
                }
                else
                { 
                    if (Cache["lastButtonID"] == null || !((String)Cache["lastButtonID"]).Equals(senderButton.ID))
                    {
                        pile.Push(senderButton.Text);
                        Cache.Insert("lastButtonID", senderButton.ID, null,
                        DateTime.Now.AddSeconds(300), TimeSpan.Zero);
                    }
                    else
                    {
                        pile.Clear();
                        Cache.Remove("lastButtonID");
                        Errors.Text = "Opération non valable : vous avez cliqué sur la même valeur";
                    }
                }

                Cache.Insert("pile", pile, null,
                    DateTime.Now.AddSeconds(300), TimeSpan.Zero);

                if (pile.Count == 3)
                {
                    Cache.Remove("lastButtonID");
                    int result = controller.calculerPile(pile);
                    if(result == -1)
                    {
                        pile.Clear();
                        Cache.Remove("lastButtonID");
                        Errors.Text = "Opération non valable";
                    } else
                    {
                        //Process the calcul
                        List<String> myOperators = new List<string>();
                        List<KeyValuePair<int, List<String>>> myValues = (List<KeyValuePair<int, List<String>>>)Cache["values"];
                        int n;
                        foreach (String item in pile)
                        {
                            if(int.TryParse(item, out n))
                            {
                                foreach (KeyValuePair<int, List<String>> value in myValues)
                                {
                                    if(value.Key == Convert.ToInt32(item))
                                    {
                                        foreach(String mot in value.Value)
                                        {
                                            myOperators.Add(mot);
                                        }
                                        myValues.Remove(value);
                                        break;
                                    }
                                }
                            }
                        }
                        
                        //Add the new value to the list of usable values
                        pile.Pop();
                        String operateur = pile.Pop();
                        pile.Pop();
                        myOperators.Add(operateur);
                        myValues.Add(new KeyValuePair<int, List<String>>(result, myOperators));
                        pile.Clear();

                        if (result == (int)Cache["solution"])
                        {
                            int score = controller.calculerScore(myOperators);

                            //Clear Cache
                            Cache.Remove("lastButtonID");
                            Cache.Remove("solution");
                            Cache.Remove("values");
                            Cache.Remove("pile");
                            Cache.Remove("initialValues");
                            Cache.Insert("score", (int)Cache["score"]+score, null,
                                DateTime.Now.AddSeconds(300), TimeSpan.Zero);

                            Page.Response.Redirect(Page.Request.Url.ToString(), true);

                            return;
                        }

                        Cache.Remove("pile");
                        Cache.Remove("lastButtonID");
                        Cache.Insert("values", myValues, null,
                        DateTime.Now.AddSeconds(300), TimeSpan.Zero);

                        setButtons();
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Response.Write(ex);
            }

        }

        public void setButtons()
        {
            if (values.Count >= 1)
            {
                Button2.Text = Convert.ToString(values[0].Key);
            }
            if (values.Count >= 2)
            {
                Button3.Text = Convert.ToString(values[1].Key);
            }
            if (values.Count >= 3)
            {
                Button4.Text = Convert.ToString(values[2].Key);
            }
            if (values.Count >= 4)
            {
                Button5.Text = Convert.ToString(values[3].Key);
            }
            if (values.Count >= 5)
            {
                Button6.Text = Convert.ToString(values[4].Key);
            }
        }

        protected void Reset_Click(object sender, EventArgs e)
        {
            Cache.Remove("lastButtonID");
            Cache.Remove("pile");

            //Reset values to initials ones
            List<KeyValuePair<int, List<String>>> initialValues = new List<KeyValuePair<int, List<String>>>((List < KeyValuePair < int, List < String >>> )Cache["initialValues"]);

            Cache.Insert("values", initialValues, null,
                DateTime.Now.AddSeconds(300), TimeSpan.Zero);
        }

        protected void saveScore(object sender, EventArgs e)
        {
            //Clear Cache
            Cache.Remove("lastButtonID");
            Cache.Remove("solution");
            Cache.Remove("values");
            Cache.Remove("pile");
            Cache.Remove("initialValues");
            Cache.Remove("score");

            //Save in DB
            controller.insertResult(Username.Text, Convert.ToInt32(Score.Text));
            Page.Response.Redirect(Page.Request.Url.ToString(), true);
        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan time = new TimeSpan();
            if(Cache["time"] != null)
            {
                time = (DateTime)Cache["time"] - DateTime.Now;
            } else
            {
                time = DateTime.Now - DateTime.Now;
            }
            
            if (time.Seconds <= 0 && time.Minutes <=0)
            {
               Chrono.Text = "TimeOut!";
            }
            else
            {
               Chrono.Text = time.Minutes.ToString() + ":" + time.Seconds.ToString();
            }
        }

        protected void setCache()
        {
            if (Cache["time"] == null)
            {
                Cache.Insert("time", DateTime.Now.AddSeconds(180), null,
                DateTime.Now.AddSeconds(200), TimeSpan.Zero);
            }

            if (Cache["solution"] == null)
            {
                int solution = moteur.getTargetNumber();
                Cache.Insert("solution", solution, null,
                DateTime.Now.AddSeconds(300), TimeSpan.Zero);
                Solution.Text = Convert.ToString(solution);
            }
            else
            {
                Solution.Text = Convert.ToString(Cache["solution"]);
            }

            if (Cache["values"] == null)
            {
                List<int> initialGenValues = moteur.getRandomNumbers();
                trySolver(initialGenValues);
                List<KeyValuePair<int, List<String>>> initialValues = new List<KeyValuePair<int, List<String>>>();
                foreach (int value in initialGenValues)
                {
                    initialValues.Add(new KeyValuePair<int, List<String>>(value, new List<string>()));
                }
                Cache.Insert("values", initialValues, null,
                DateTime.Now.AddSeconds(300), TimeSpan.Zero);
                Cache.Insert("initialValues", new List<KeyValuePair<int, List<String>>>(initialValues), null,
                DateTime.Now.AddSeconds(300), TimeSpan.Zero);
            }

            values = (List<KeyValuePair<int, List<String>>>)Cache["values"];

            if (Cache["score"] != null)
            {
                Score.Text = Convert.ToString(Cache["score"]);
            }
            else
            {
                Cache.Insert("score", 0, null,
                                DateTime.Now.AddSeconds(300), TimeSpan.Zero);
                Score.Text = Convert.ToString(Cache["score"]);
            }
            

            if (Cache["pile"] == null)
            {
                Cache.Insert("pile", pile, null,
                    DateTime.Now.AddSeconds(300), TimeSpan.Zero);
            }
        }

        public void trySolver(List<int> generatedValues)
        {
            List<String> results = new List<string>();
            string calcul = "";
            //Start solveur
            moteur.Solveur(results, generatedValues, (int)Cache["solution"], calcul);
            //Write solutions in file
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = folder + "/solutions.txt";
            File.AppendAllLines(path, results);
        }
    }
}