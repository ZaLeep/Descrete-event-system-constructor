using System;
using System.Collections.Generic;
using System.Text;

namespace Lab2
{
    class Model
    {
        private List<Component> model;
        private double currTime, nextTime;

        public Model()
        {
            model = new List<Component>();
            nextTime = 0.0;
            currTime = 0.0;
        }

        public void SetNextElement(Component c)
        {
            if (model.Count != 0)
                model[model.Count - 1].SetNext(c);
            model.Add(c);
        }
        public void AddNextElement(Component c)
        {
            if (model.Count != 0)
                model[model.Count - 1].AddNext(c);
            model.Add(c);
        }
        public void AddElement(Component c)
        {
            model.Add(c);
        }
        public void Simulate(double duration)
        {
            while(currTime < duration)
            {
                Component currComponent = null;
                nextTime = Double.MaxValue;
                foreach(Component c in model)
                {
                    if(c.GetNextTime() < nextTime)
                    {
                        nextTime = c.GetNextTime();
                        currComponent = c.GetNextComponent();
                    }
                }
                Console.WriteLine($"____________________It's {nextTime} on the clock and it's time for {currComponent.name}____________________");
                
                foreach (Component c in model)
                    c.GetStatistics(nextTime - currTime);
                
                currTime = nextTime;
                foreach (Component c in model)
                    c.SetCurrTime(currTime);

                foreach (Component c in model)
                    if (c.GetNextTime() == currTime)
                        c.OutAct();

                foreach (Component c in model)
                {
                    c.GetState();
                    Console.WriteLine("||\n\\/");
                }
                Console.WriteLine("End.\n\n");
            }
            GetResult();
        }

        private void GetResult()
        {
            foreach (Component c in model) c.SetCurrTime(currTime);
                Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>RESULTS<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            foreach(Component c in model)
            {
                c.GetResult();
                Console.WriteLine("||\n\\/");
            }
            Console.WriteLine("End.\n");
        }
    }
}
