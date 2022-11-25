using System;
using System.Collections.Generic;

namespace Lab2
{
    public class Task
    {
        public string name;
        public double startTime = 0;
        public double processingDelay, addProcessingDelay;
        Stack<Component> path;

        public Component Path
        {
            get => path.Peek();
            set => path.Push(value);
        }

        public Task(string name = "#NoName Task", double processingDelay = -1, double addProcessingDelay = -1)
        {
            path = new Stack<Component>();
            this.name = name;
            this.processingDelay = processingDelay;
            this.addProcessingDelay = addProcessingDelay;
        }

        public Task Copy()
        {
            return new Task(this.name, this.processingDelay, this.addProcessingDelay);
        }

        public Component KickBack() => path.Pop();
    }

    public abstract class Component
    {
        protected static Random rand = new Random();

        protected List<Component> nextComponent;
        protected Component prevComponent;
        protected double currTime, nextTime, avgQueue;
        protected double basicDelay, additionalDelay;
        protected QueuePickCondition queuePickCondition;
        protected ChooseCondition chooseCondition;
        protected Action addStatisicAction;
        protected int itemsCount, dropsOut;
        public string name, distribution;
        protected List<int> chances;
        protected List<Task> tasks;
        protected bool selection;
        protected List<double> buffer = new List<double>();

        public double NextTime
        {
            get => nextTime;
            set => nextTime = value;
        }
        public virtual bool FirstLevelAccess
        {
            get => true;
        }
        public virtual int SecondLevelAccess
        {
            get => Int16.MaxValue;
        }
        public virtual int Queue
        {
            get => 0;
            set { }
        }
        public Component Sender
        {
            get => tasks[tasks.Count - 1].Path;
        }
        public List<Component> Receivers
        {
            get => nextComponent;
        }
        public bool SelectionFlag
        {
            get => selection;
            set => selection = value;
        }
        public Component Previous
        {
            get => prevComponent;
            set
            {
                if (prevComponent is null)
                    prevComponent = value;
            }
        }

        public Component(string name = "NoName", double delay = 1.0, double addDelay = 0.5, string d = "exp", bool selection = true)
        {
            nextComponent = new List<Component>();
            chances = new List<int>();
            tasks = new List<Task>();
            chooseCondition = DefaultSelectionCondition;
            queuePickCondition = DefaultQueuePickCondition;
            addStatisicAction = null;
            additionalDelay = addDelay;
            this.selection = selection;
            basicDelay = delay;
            this.name = name;
            distribution = d;
            currTime = 0.0;
            nextTime = 0.0;
            dropsOut = 0;
        }

        /// <summary>
        /// Integers representation of chances ratio.
        /// If there no params - all receivers gets equal chance ration.
        /// If there not enough params - rest components have 1 ratio chance.
        /// If there more params than needs - rest params will be ignored.
        /// </summary>
        public void SetChoosingChances(params int[] chances)
        {
            this.chances.Clear();
            for (int i = 0; i < nextComponent.Count; i++)
                this.chances.Add(1);
            int index = 0;
            while(index < chances.Length && index < this.chances.Count)
            {
                this.chances[index] = chances[index];
                index++;
            }
        }

        protected Component ChoosingByChances()
        {
            List<Component> enableComponents = new List<Component>();
            List<int> enableChances = new List<int>();
            for(int i = 0; i < nextComponent.Count; i++)
            {
                if(nextComponent[i].FirstLevelAccess || nextComponent[i].SecondLevelAccess > 0)
                {
                    enableComponents.Add(nextComponent[i]);
                    enableChances.Add(chances[i]);
                }
            }

            int r = rand.Next(Sum(enableChances));
            for(int i = 0; i < enableChances.Count; i++)
            {
                r -= enableChances[i];
                if (r < 0)
                    return enableComponents[i];
            }
            return null;
        }

        public void SetChooseCondition(ChooseCondition cond) => chooseCondition = cond;

        public void SetQueuePickCondition(QueuePickCondition cond) => queuePickCondition = cond;

        public void SetAdditionalStatisticAction(Action action) => addStatisicAction = action;

        protected double GetDelay(double delay, double addDelay)
        {
            delay = (delay == -1) ? this.basicDelay : delay;
            addDelay = (addDelay == -1) ? this.additionalDelay : addDelay;
            switch (distribution)
            {
                case "exp":
                    return Generator.Exp(delay);
                case "unif":
                    return Generator.Unif(delay, addDelay);
                case "norm":
                    return Generator.Norm(delay, addDelay);
                case "erl":
                    return Generator.Norm(delay, (int)addDelay);
                default:
                    return Generator.Exp(delay);
            }
            return basicDelay;
        }

        public virtual double GetNextTime() => nextTime;

        public virtual void OutAct()
        {
            itemsCount++;
        }

        public abstract int InAct(Task task);

        internal virtual void SetCurrTime(double time) => currTime = time;
        
        internal void SetDistributionType(string d) => distribution = d;

        public virtual void GetStatistics(double d) { }

        public abstract void GetState();

        public abstract void GetResult();

        public virtual void SetNext(params Component[] components)
        {
            nextComponent.Clear();
            foreach (Component c in components)
            {
                nextComponent.Add(c);
                c.Previous = this;
            }
            SetChoosingChances();
        }

        public virtual void AddNext(Component n)
        {
            nextComponent.Add(n);
            chances.Add(Avg(chances));
            n.Previous = this;
        }

        public virtual void ClearNext()
        {
            nextComponent.Clear();
            chances.Clear();
        }

        public virtual Component GetNextComponent() => this;

        public virtual void CheckStepsBack() { }

        public void KickBack()
        {
            Task last = tasks[tasks.Count - 1];
            tasks.RemoveAt(tasks.Count - 1);
            last.KickBack().InAct(last);
        }

        protected Component DefaultSelectionCondition(Task forSending, List<Component> components)
        {
            if (components is null || components.Count == 0)
            {
                Console.WriteLine("Something wrong: Receiver List is null or empty.");
                return null;
            }

            foreach (Component p in nextComponent)
            {
                if (p.FirstLevelAccess)
                    return p;
            }

            Component selected = null;
            int max = 0;
            foreach (Component p in nextComponent)
            {
                if (p.SecondLevelAccess > max)
                {
                    selected = p;
                    max = p.SecondLevelAccess;
                }
            }
            if (selected is null)
                return null;
            return selected;
        }
        protected int DefaultQueuePickCondition(List<Task> tasks) => 0;
        protected static int Avg(List<int> chances) => Sum(chances) / chances.Count;
        protected static int Sum(List<int> chances)
        {
            int sum = 0;
            foreach (int i in chances)
                sum += i;
            return sum;
        }
    }
    class Create : Component
    {
        List<Task> basicTasks = new List<Task>();
        List<int> taskChances = new List<int>();


        public Create(string name, double delay, Component receiver = null, Task example = null):base(name, delay)
        {
            basicTasks.Add((example is null) ? new Task(name + " product", delay) : example);
            if (!(receiver is null)) nextComponent.Add(receiver);
            SetCreatingChances();
            SetChoosingChances();
        }

        public void SetTask(Task example)
        {
            basicTasks.Add(example);
            taskChances.Add(1);
        }

        public void SetCreatingChances(params int[] chances)
        {
            this.taskChances.Clear();
            for (int i = 0; i < basicTasks.Count; i++)
                this.taskChances.Add(1);
            int index = 0;
            while (index < chances.Length && index < this.taskChances.Count)
            {
                this.taskChances[index] = chances[index];
                index++;
            }
        }

        public override int InAct(Task task) => -1;

        public override void OutAct()
        {
            base.OutAct();
            Task product = GetProduct();
            product.startTime = currTime;
            product.Path = this;
            if (nextComponent.Count > 0)
            {
                nextTime = currTime + base.GetDelay(-1, -1);
                if (nextComponent.Count == 1)
                    nextComponent[0].InAct(product);
                else
                {
                    Component toWork = (selection) ? chooseCondition(product, nextComponent) : ChoosingByChances();
                    if (toWork is null)
                        dropsOut++;
                    else
                        toWork.InAct(product);
                }
            }
            else Console.WriteLine("Something wrong: Create block has no receiver.");
        }

        private Task GetProduct()
        {
            int r = rand.Next(Sum(taskChances));
            for (int i = 0; i < taskChances.Count; i++)
            {
                r -= taskChances[i];
                if (r < 0)
                    return basicTasks[i].Copy();
            }
            return null;
        }
        public override void GetState() => Console.WriteLine($"Creation block: {name} > item passed = {itemsCount} | next action at: {nextTime}s;");

        public override void GetResult() => Console.WriteLine($"Creation block: {name} > item passed = {itemsCount};");
    }
    class Process: Component
    {
        protected int queue, queueLimit;
        private bool isAccessible, innerQueue;
        private KickBackCondition? cond;
        public override int Queue
        {
            get => queue;
            set
            {
                queue = value;
                List<Task> newTasks = new List<Task>();
                newTasks.Add(tasks[0]);
                for (int i = 0; i < value; i++)
                {
                    Task newTask = new Task();
                    newTask.Path = Previous;
                    newTasks.Add(newTask);

                }
                tasks = newTasks;
            }
        }
        public bool Access
        {
            set
            {
                isAccessible = value;
                if (!value)
                {
                    tasks.Add(new Task());
                    nextTime = currTime + GetDelay(tasks[0].processingDelay, tasks[0].addProcessingDelay);
                }
                else
                    tasks.RemoveAt(0);
            }
        }
        public override bool FirstLevelAccess
        {
            get => isAccessible;
        }
        public override int SecondLevelAccess
        {
            get => innerQueue ? queueLimit - queue : 0;
        }

        public Process(string name, double delay = 1.0, double addDelay = 0.5, string distribution = "exp", int queueLimit = Int32.MaxValue, Component receiver = null, bool innerQueue = true) : base(name, delay, addDelay, distribution)
        {
            nextTime = Double.MaxValue;
            queue = 0;
            this.queueLimit = queueLimit;
            this.innerQueue = innerQueue;
            if (queueLimit <= 0) this.innerQueue = false;
            isAccessible = true;
            if (!(receiver is null)) nextComponent.Add(receiver);
            SetChoosingChances();
            cond = null;
            buffer.Add(0);
            buffer.Add(0);
            buffer.Add(-1);
            buffer.Add(0);
        }

        public void SetCondition(KickBackCondition c) => cond = c;

        public override int InAct(Task task)
        {
            if(isAccessible)
            {
                isAccessible = false;
                tasks.Add(task);
                nextTime = currTime + GetDelay(task.processingDelay, task.addProcessingDelay);
            }
            else
            {
                if (innerQueue && queue < queueLimit)
                {
                    tasks.Add(task);
                    queue++;
                }
                else
                {
                    dropsOut++;
                    return -1;
                }
            }
            return 1;
        }

        public override void OutAct()
        {
            if(!isAccessible) base.OutAct();

            Task forSending = tasks[0];
            forSending.Path = this;
            tasks.RemoveAt(0);

            if (queue != 0)
            {
                queue--;
                isAccessible = false;
                int chosenTaskIndex = queuePickCondition(tasks);
                Task buffer = tasks[0];
                tasks[0] = tasks[chosenTaskIndex];
                tasks[chosenTaskIndex] = buffer;
                
                nextTime = currTime + base.GetDelay(tasks[0].processingDelay, tasks[0].addProcessingDelay);
            }
            else
            {
                nextTime = Double.MaxValue;
                isAccessible = true;
            }
            if (nextComponent.Count > 0)
            {
                if (nextComponent.Count == 1)
                    nextComponent[0].InAct(forSending);
                else
                    if (selection)
                        chooseCondition(forSending, nextComponent).InAct(forSending);
                    else
                        ChoosingByChances().InAct(forSending);
            }
        }

        public override void CheckStepsBack()
        {
            if (!(cond is null))
            {
                while (queue > 0 && cond(this))
                {
                    queue--;
                    buffer[1]++;
                    KickBack();
                }
            }
        }

        public override void GetStatistics(double d)
        {
            avgQueue += queue * d;
            if (!isAccessible)
                buffer[0] += d;
        }

        public override void GetState() =>
            Console.WriteLine("Process: {0} > next action at: {1}s | isAccesible: {2} | queue: {3} | kickBacks: {4};", name, (nextTime == Double.MaxValue) ? -1 : nextTime, isAccessible, queue, buffer[1]);

        public override void GetResult()
        {
            Console.WriteLine($"Process: {name} > item passed: {itemsCount} | Average load: {Math.Round(buffer[0] / currTime * 100, 2)}% | Average queue: {avgQueue / currTime};");
            Console.WriteLine($"Total kickBacks: {buffer[1]} | drops out: {dropsOut} | items losing probability: {dropsOut / ((double)itemsCount + dropsOut)}");
        }
    }
    class ProcessBlock: Process
    {
        private Component nearestComponent;

        public override bool FirstLevelAccess
        {
            get {
                foreach (Process p in nextComponent) if(p.FirstLevelAccess) return true;
                return false;
            }
        }
        public override int SecondLevelAccess
        {
            get => queueLimit - queue;
        }
        
        public ProcessBlock(string name, int queueLimit = Int32.MaxValue, Component receiver = null, params Process[] workers) : base(name, 1.0, 0.5, "exp", queueLimit, receiver)
        {
            buffer.Clear();
            buffer.Add(0);
            buffer.Add(-1);
            buffer.Add(0);
            if (!(workers is null))
            {
                foreach (Process p in workers)
                {
                    nextComponent.Add(p);
                    p.Previous = this;
                }
            }
            SetChoosingChances();
        }

        internal override void SetCurrTime(double time)
        {
            foreach (Process p in nextComponent) 
                p.SetCurrTime(time);
            currTime = time;
        }

        public override int InAct(Task task)
        {
            if (buffer[1] == -1)
                buffer[1] = currTime;
            else
            {
                buffer[2] += currTime - buffer[1];
                buffer[1] = currTime;
            }
            Component toWork = (selection) ? chooseCondition(task, nextComponent) : ChoosingByChances();

            if (toWork is null)
            {
                if (queue < queueLimit)
                {
                    tasks.Add(task);
                    queue++;
                }
                else
                {
                    dropsOut++;
                    return -1;
                }
            }
            else
            {
                task.Path = this;
                toWork.InAct(task);
            }
            return 1;
        }

        public override void OutAct()
        {
            foreach (Component p in nextComponent)
                if (p.GetNextTime() == nextTime)
                {
                    p.OutAct();
                    itemsCount++;
                    if (queue != 0)
                    {
                        int chosenTaskIndex = queuePickCondition(tasks);
                        tasks[chosenTaskIndex].Path = this;
                        p.InAct(tasks[chosenTaskIndex]);
                        tasks.RemoveAt(chosenTaskIndex);
                        queue--;
                    }
                }
            foreach (Component c in nextComponent)
                c.CheckStepsBack();
        }

        public override double GetNextTime()
        {
            nextTime = Double.MaxValue;
            foreach (Component p in nextComponent)
            {
                if (p.GetNextTime() <= nextTime)
                {
                    nextTime = p.GetNextTime();
                    nearestComponent = p.GetNextComponent();
                }
            }
            return nextTime;
        }
        
        public override Component GetNextComponent() => nearestComponent;

        public override void GetStatistics(double d)
        {
            foreach(Process p in nextComponent)
            {
                p.GetStatistics(d);
                if (p.FirstLevelAccess)
                    buffer[0] += p.Queue * d;
                else
                    buffer[0] += (p.Queue + 1) * d;
            }
            avgQueue += queue * d;
        }

        public override void GetState()
        {
            Console.WriteLine("===========================================================================================");
            for (int i = 0; i < nextComponent.Count - 1; i++)
            {
                nextComponent[i].GetState();
                Console.WriteLine("||");
            }
            nextComponent[nextComponent.Count - 1].GetState();
            Console.WriteLine("-------------------------------------------------------------------------------------------");
            Console.WriteLine("\nProcess block: {0} > next action at: {1}s | queue: {2};", name, (nextTime == Double.MaxValue) ? -1 : nextTime, queue);
            Console.WriteLine("===========================================================================================");
        }

        public override void GetResult()
        {
            Console.WriteLine("===========================================================================================");
            for (int i = 0; i < nextComponent.Count - 1; i++)
            {
                nextComponent[i].GetResult();
                Console.WriteLine("||");
            }
            nextComponent[nextComponent.Count - 1].GetResult();
            Console.WriteLine("-------------------------------------------------------------------------------------------");
            Console.WriteLine($"\nProcess block: {name} > item passed: {itemsCount} | drops out: {dropsOut}; | AverageTime between units: {buffer[2] / itemsCount + queue}");
            Console.WriteLine($"Average queue: {avgQueue / currTime} | Average unit quantity: {buffer[0] / currTime}");
            Console.WriteLine("===========================================================================================");
        }

        public override void SetNext(params Component[] components)
        {
            foreach (Process p in nextComponent)
                p.SetNext(components);
        }

        public override void AddNext(Component component)
        {
            foreach (Process p in nextComponent)
                p.AddNext(component);
        }

        public override void ClearNext()
        {
            foreach (Process p in nextComponent)
                p.ClearNext();
        }
    }
    class Counter : Component
    {
        List<double> nextTimes;
        private bool delayBlock;
        public bool Delaying { get => delayBlock; set => delayBlock = value; }
        public Counter(string name, double delay = 1.0, double addDelay = 0.5, string distribution = "exp") : base(name, delay, addDelay, distribution)
        {
            delayBlock = false;
            nextTimes = new List<double>();
            buffer.Add(0);
            buffer.Add(-1);
            buffer.Add(0);
        }

        public override int InAct(Task task)
        {
            tasks.Add(task);
            if (delayBlock)
            {
                nextTimes.Add(currTime + GetDelay(task.processingDelay, task.addProcessingDelay));
                FindNext();
            }
            itemsCount++;
            buffer[0] += currTime - task.startTime;
            if (buffer[1] == -1)
                buffer[1] = currTime;
            else
            {
                buffer[2] += currTime - buffer[1];
                buffer[1] = currTime;
            }
            if(!delayBlock)
                OutAct();
            return 1;
        }

        public override void OutAct()
        {
            if (nextComponent.Count > 0)
            {
                if (nextComponent.Count == 1)
                    nextComponent[0].InAct(tasks[(int)nextTime]);
                else
                    if (selection)
                        chooseCondition(tasks[(int)nextTime], nextComponent).InAct(tasks[(int)nextTime]);
                    else
                        ChoosingByChances().InAct(tasks[(int)nextTime]);
            }
            if (delayBlock)
            {
                tasks.RemoveAt((int)nextTime);
                nextTimes.RemoveAt((int)nextTime);
                nextTime = -1;
                FindNext();
            }
        }

        public override void GetState()
        {
            Console.WriteLine($"Counter: {name} > Item passed: {itemsCount}");
            if(delayBlock)
            {
                Console.Write($"Inner items: {nextTimes.Count}");
                if (nextTimes.Count != 0)
                    Console.WriteLine($" | Next action: {nextTimes[(int)nextTime]}");
                else
                    Console.WriteLine();
            }
        }

        public override void GetResult()
        {
            Console.WriteLine($"Counter: {name} > Item passed: {itemsCount}");
            if(!delayBlock)
                Console.WriteLine($"AvgInterval between units: {buffer[2] / itemsCount} | AvgTime per unit: {buffer[0] / itemsCount}s");
        }

        public override double GetNextTime()
        {
            if(!delayBlock || nextTimes.Count == 0)
                return Double.MaxValue;
            return nextTimes[(int)nextTime];
        }

        private void FindNext()
        {
            if (nextTime == -1 && nextTimes.Count > 0)
                nextTime = 0;
            for (int i = 0; i < nextTimes.Count; i++)
            {
                if (nextTimes[i] < nextTimes[(int)nextTime])
                    nextTime = i;
            }
        }
    }

    public delegate Component ChooseCondition(Task forSending, List<Component> components);
    public delegate bool KickBackCondition(Component component);
    public delegate int QueuePickCondition(List<Task> tasks);
}
