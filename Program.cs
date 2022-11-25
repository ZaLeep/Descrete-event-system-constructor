using System;
using System.Collections.Generic;

namespace Lab2
{
    class Program
    {
        static void Main(string[] args)
        {
            Model model = new Model();

            Task cure1 = new Task("Sick#1", 15);
            Task cure2 = new Task("Sick#2", 40);
            Task cure3 = new Task("Sick#3", 30);

            Create creator = new Create("Creator", 15, example: cure1);
            creator.SetTask(cure2);
            creator.SetTask(cure3);
            creator.SetCreatingChances(5, 1, 4);
            model.SetNextElement(creator);

            Process duty1 = new Process("DutyDoctorx#1", distribution: "exp", queueLimit: 0);
            duty1.SetChooseCondition(SelectionCondition1);
            Process duty2 = new Process("DutyDoctorx#2", distribution: "exp", queueLimit: 0);
            duty2.SetChooseCondition(SelectionCondition1);

            ProcessBlock dutyBase = new ProcessBlock("DutyBase", workers: new Process[] { duty1, duty2 });
            dutyBase.SetQueuePickCondition(QueuePickConditional);
            model.SetNextElement(dutyBase);

            Counter firstCoridor = new Counter("Coridor#1", 2, 5, "unif");
            firstCoridor.Delaying = true;
            dutyBase.SetNext(firstCoridor);
            model.AddElement(firstCoridor);

            Process register = new Process("Register", 4.5, 3, "erl");
            firstCoridor.SetNext(register);
            model.AddElement(register);

            Counter secondCoridor = new Counter("Coridor#2", 2, 5, "unif");
            secondCoridor.Delaying = true;
            register.SetNext(secondCoridor);
            model.AddElement(secondCoridor);

            Process lab1 = new Process("Laboratory#1", 4, 3, distribution: "erl", queueLimit: 0);
            lab1.SetChooseCondition(SelectionCondition2);
            Process lab2 = new Process("Laboratory#2", 4, 3, distribution: "erl", queueLimit: 0);
            lab2.SetChooseCondition(SelectionCondition2);

            ProcessBlock lab = new ProcessBlock("Laboratory", workers: new Process[] { lab1, lab2 });
            secondCoridor.SetNext(lab);
            lab.SetNext(dutyBase);
            model.AddElement(lab);

            Process convoy1 = new Process("Convoy#1", 3, 8, distribution: "unif", queueLimit: 0);
            Process convoy2 = new Process("Convoy#2", 3, 8, distribution: "unif", queueLimit: 0);
            Process convoy3 = new Process("Convoy#3", 3, 8, distribution: "unif", queueLimit: 0);
            ProcessBlock convoy = new ProcessBlock("ConvoyBand", workers: new Process[] { convoy1, convoy2, convoy3 });
            dutyBase.AddNext(convoy);
            model.AddElement(convoy);

            Counter final = new Counter("Exit");
            lab.AddNext(final);
            convoy.SetNext(final);
            model.AddElement(final);

            model.Simulate(1000);

            Component SelectionCondition1(Task forSending, List<Component> components)
            {
                forSending.processingDelay = -1;
                if (forSending.name.EndsWith('1'))
                    return components[1];
                else
                    return components[0];
            }

            Component SelectionCondition2(Task forSending, List<Component> components)
            {
                if (forSending.name.EndsWith('3'))
                    return components[1];
                else
                {
                    forSending.name = "Sick#1";
                    forSending.processingDelay = 15;
                    return components[0];
                }
            }

            int QueuePickConditional(List<Task> list)
            {
                for(int i = 0; i < list.Count; i++)
                {
                    if (list[i].name.EndsWith('1'))
                        return i;
                }
                return 0;
            }
        }
    }
}
