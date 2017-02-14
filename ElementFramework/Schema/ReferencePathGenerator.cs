using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Schema
{
    internal class ReferencePathGenerator
    {
        protected class Node
        {
            public string From { get; private set; }
            public string Content { get; private set; }
            public string To { get; private set; }

            public int Weight { get; private set; }
            public int TotalWeight { get; private set; }

            public Node Parent { get; private set; }

            private List<Node> _children = new List<Node>();
            public IEnumerable<Node> Children { get { return _children; } }

            public bool IsYellow { get; private set; }

            private List<string> _elementNames = new List<string>();

            public Node(string from, string to, string content, int weight)
            {
                From = from;
                To = to;
                Content = content;
                Weight = weight;
            }

            public static Node CreateRoot(string from, string to, string content, int weight)
            {
                Node root = new Node(from, to, content, weight);
                root._elementNames.Add(from);
                root._elementNames.Add(to);
                root.TotalWeight = weight;
                return root;
            }

            public void Add(Node child)
            {
                if (_elementNames.Contains(child.To)) return; // cycle

                //
                IEnumerable<Node> nodeChildren = Children.Where(p => p.To == child.To && !p.IsYellow);
                foreach (Node nodeChild in nodeChildren)
                {
                    if (nodeChild.Weight > child.Weight) return;
                    if (nodeChild.Weight == child.Weight)
                    {
                        nodeChild.IsYellow = true;
                        return;
                    }
                }

                //
                child.Parent = this;
                _children.Add(child);

                child._elementNames = new List<string>(_elementNames);
                child._elementNames.Add(child.To);

                child.TotalWeight = TotalWeight + child.Weight;
            }
        }

        public const int Default_MaxLevel = 5;

        protected const int Prime_Relationship_Weight = 3;
        protected const int Prime_Reference_Weight = 2;

        protected XElement Schema;
        protected int MaxLevel;

        // ToOne
        protected List<XElement> Relationships = new List<XElement>();

        public ReferencePathGenerator(XElement schema)
            : this(schema, Default_MaxLevel)
        {
        }

        public ReferencePathGenerator(XElement schema, int maxLevel)
        {
            Schema = new XElement(schema);
            MaxLevel = maxLevel;

            //
            Dictionary<string, XElement> relatDict = new Dictionary<string, XElement>();
            IEnumerable<XElement> fromtoRelationships = Schema.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null &&
                (x.Attribute(Glossary.RelationshipType).Value == "ManyToOne" || x.Attribute(Glossary.RelationshipType).Value == "OneToOne"));
            foreach (XElement relationship in fromtoRelationships)
            {
                string content = relationship.Attribute(Glossary.RelationshipContent).Value;
                bool isPrime = relationship.Element(Glossary.Prime) != null;

                XElement relationship2 = null;
                if (relatDict.ContainsKey(content))
                {
                    relationship2 = relatDict[content];
                }
                else
                {
                    relationship2 = relationship;
                    relatDict[content] = relationship2;
                }
                if (isPrime)
                {
                    relationship2.SetAttributeValue("Weight", Prime_Relationship_Weight);
                }
            }

            // Reverse
            IEnumerable<XElement> tofromRelationships = Schema.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null &&
                (x.Attribute(Glossary.RelationshipType).Value == "OneToMany" || x.Attribute(Glossary.RelationshipType).Value == "OneToOne"));
            foreach (XElement tofromRelationship in tofromRelationships)
            {
                XElement relationship = tofromRelationship.CreateRelationship().Reverse().ToElement();
                string content = relationship.Attribute(Glossary.RelationshipContent).Value;
                bool isPrime = tofromRelationship.Element(Glossary.Prime) != null;

                XElement relationship2 = null;
                if (relatDict.ContainsKey(content))
                {
                    relationship2 = relatDict[content];
                }
                else
                {
                    relationship2 = relationship;
                    relatDict[content] = relationship2;
                }
                if (isPrime)
                {
                    relationship2.SetAttributeValue("Weight", Prime_Relationship_Weight);
                }
            }

            // ReferencePath as Relationship
            Dictionary<string, XElement> referDict = new Dictionary<string, XElement>();
            IEnumerable<XElement> referencePaths = Schema.Elements(Glossary.ReferencePath);
            foreach (XElement referencePath in referencePaths)
            {
                string content = referencePath.Attribute(Glossary.RelationshipContent).Value;
                bool isPrime = referencePath.Element(Glossary.Prime) != null;

                XElement relationship2 = null;
                if (referDict.ContainsKey(content))
                {
                    if (isPrime)
                    {
                        if (referDict[content].Attribute("Weight") == null)
                        {
                            relationship2 = referDict[content];
                        }
                    }
                }
                else
                {
                    relationship2 = new XElement(referencePath);
                    relationship2.Name = Glossary.Relationship;
                    relationship2.SetAttributeValue(Glossary.RelationshipType, "ManyToOne");
                    relatDict[content] = relationship2;
                }
                if (isPrime && relationship2 != null)
                {
                    int weight = content.Split(';').Length * Prime_Reference_Weight;
                    relationship2.SetAttributeValue("Weight", weight);
                }
            }

            //
            Relationships.AddRange(relatDict.Values);
            Relationships.AddRange(referDict.Values);
        }

        public IEnumerable<XElement> Generate(string from, string to)
        {
            List<XElement> result = new List<XElement>();

            List<Node> greenLeaves = new List<Node>();
            IEnumerable<XElement> relationships = Relationships.Where(x => x.Attribute(Glossary.RelationshipFrom).Value == from);
            foreach (XElement relationship in relationships)
            {
                string nodeTo = relationship.Attribute(Glossary.RelationshipTo).Value;

                int weight = 0;
                if (relationship.Attribute("Weight") != null)
                {
                    weight = int.Parse(relationship.Attribute("Weight").Value);
                }

                Node node = Node.CreateRoot(from, nodeTo, relationship.Attribute(Glossary.RelationshipContent).Value, weight);
                greenLeaves.Add(node);
            }

            //
            bool isFound = false;
            for (int i = 1; i < MaxLevel; i++)
            {
                greenLeaves = GenerateGreenLeaves(greenLeaves, to, ref isFound);
                if (isFound) break;
            }
            if (!isFound) return result;

            //
            List<Node> nodes = new List<Node>();
            IEnumerable<Node> found = greenLeaves.Where(p => p.To == to);
            if (found.Count() == 1)
            {
                nodes.Add(found.First());
            }
            else
            {
                found = found.OrderByDescending(p => p.TotalWeight);
                int firstTotalWeight = found.First().TotalWeight;
                found = found.Where(p => p.TotalWeight == firstTotalWeight);
                nodes.AddRange(found);
            }

            //
            foreach (Node node in nodes)
            {
                List<string> contents = new List<string>();

                Node node2 = node;
                while (node2 != null)
                {
                    contents.Add(node2.Content);
                    node2 = node2.Parent;
                }
                contents.Reverse();
                string pathContent = string.Join(";", contents);

                XElement referencePath = new XElement(Glossary.ReferencePath);
                referencePath.SetAttributeValue(Glossary.RelationshipFrom, from);
                referencePath.SetAttributeValue(Glossary.RelationshipTo, to);
                referencePath.SetAttributeValue(Glossary.RelationshipContent, pathContent);
                result.Add(referencePath);
            }

            return result;
        }

        protected List<Node> GenerateGreenLeaves(List<Node> greenLeaves, string to, ref bool isFound)
        {
            List<Node> leaves = new List<Node>();
            foreach (Node greenleave in greenLeaves)
            {
                string from = greenleave.To;

                Dictionary<string, int> to_weights = new Dictionary<string, int>();
                IEnumerable<XElement> relationships = Relationships.Where(x => x.Attribute(Glossary.RelationshipFrom).Value == from);
                foreach (XElement relationship in relationships)
                {
                    string nodeTo = relationship.Attribute(Glossary.RelationshipTo).Value;
                    if (nodeTo == to) isFound = true;

                    //
                    int weight = 0;
                    if (relationship.Attribute("Weight") != null)
                    {
                        weight = int.Parse(relationship.Attribute("Weight").Value);
                    }

                    Node node = new Node(from, nodeTo, relationship.Attribute(Glossary.RelationshipContent).Value, weight);
                    greenleave.Add(node);

                }
                leaves.AddRange(greenleave.Children.Where(p => !p.IsYellow));
            }
            return leaves;
        }


    }
}
