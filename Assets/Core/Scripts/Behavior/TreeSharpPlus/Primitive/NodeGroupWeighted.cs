using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TreeSharpPlus.ExtensionMethods;

namespace TreeSharpPlus
{
    public abstract class NodeGroupWeighted : NodeGroup
    {
        public List<float> Weights { get; set; }

        /// <summary>
        /// Shuffles the children using the given weights
        /// </summary>
        protected void Shuffle()
        {
            //this.Children.Shuffle(this.Weights);
            {
                
                System.Random rng = new System.Random();
                
                // Iterate through the list and build a range list (0..n-1) and count
                // the weight total
                double total = 0.0;
                List<int> unused = new List<int>(this.Children.Count);
                for (int i = 0; i < this.Children.Count; i++)
                {
                    total += this.Weights[i];
                    unused.Add(i);
                }

                // Now, perform the shuffle
                List<Node> order = new List<Node>(Children.Count);
                while (unused.Count > 0)
                {
                    double subtotal = 0.0;
                    double next = rng.NextDouble() * total;

                    // The node we selected for the next child
                    int selected = -1;

                    // Look through all of the unused children remaining
                    foreach (int unusedchild in unused)
                    {
                        // If we can overtake the random value with the weight mass
                        // of this particular child, select it
                        double weight = this.Weights[unusedchild];
                        if ((subtotal + weight) >= next)
                        {
                            selected = unusedchild;
                            break;
                        }

                        // Otherwise, add to the subtotal and keep going
                        subtotal += weight;
                    }

                    // Add the child we selected
                    order.Add(this.Children[selected]);

                    // Remove the weight for de-facto renormalization
                    total -= this.Weights[selected];

                    // Remove the child from consideration
                    unused.Remove(selected);
                }

                this.Children.Clear();
                foreach (Node val in order)
                    this.Children.Add(val);
            }
        }

        /// <summary>
        /// Initializes, fully normalized with no given weights
        /// </summary>
        public NodeGroupWeighted(params Node[] children)
            : base(children)
        {
            this.Weights = new List<float>();
            for (int i = 0; i < this.Children.Count; i++)
                this.Weights.Add(1.0f);
        }

        public NodeGroupWeighted(params NodeWeight[] weightedchildren)
        {
            // Initialize the base Children list and our new Weights list
            this.Children = new List<Node>();
            this.Weights = new List<float>();

            // Unpack the pairs and store their individual values
            foreach (NodeWeight weightedchild in weightedchildren)
            {
                this.Children.Add(weightedchild.Composite);
                this.Weights.Add(weightedchild.Weight);
            }
        }
    }

    /// <summary>
    /// A simple pair class for composites and weights, used for stochastic control nodes
    /// </summary>
    public class NodeWeight
    {
        public Node Composite { get; set; }
        public float Weight { get; set; }

        public NodeWeight(float weight, Node composite)
        {
            this.Composite = composite;
            this.Weight = weight;
        }
    }
}
