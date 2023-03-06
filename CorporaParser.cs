using System.Xml;

namespace Task1;

public class CorporaParser
{
    public Dictionary<string, HashSet<WordForm>> LemmatizedForms = new();

    public CorporaParser(string filename)
    {
        WordForm? currentForm = null;
        XmlTextReader? reader = null;
        try
        {
            reader = new XmlTextReader(filename);
            reader.WhitespaceHandling = WhitespaceHandling.None;
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                    {
                        switch (reader.Name)
                        {
                            case "dictionary":
                            {
                                Console.WriteLine("Parsing started");
                                break;
                            }
                            case "lemma":
                            {
                                currentForm = new WordForm();
                                int.Parse(reader.GetAttribute(0));
                                break;
                            }
                            case "l":
                            {
                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();
                                    if (reader.Name.Equals("t"))
                                    {
                                        currentForm.InitialWord = reader.Value;
                                    }

                                    reader.MoveToElement();
                                }

                                break;
                            }
                            case "g":
                            {
                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();
                                    if (reader.Name.Equals("v"))
                                    {
                                        currentForm.Properties.Add(reader.Value);
                                    }

                                    reader.MoveToElement();
                                }

                                break;
                            }
                        }
                    }
                        break;
                    case XmlNodeType.EndElement:
                    {
                        switch (reader.Name)
                        {
                            case "l":
                            {
                                HashSet<WordForm> considered;
                                if (LemmatizedForms.TryGetValue(currentForm.InitialWord, out considered))
                                {
                                    considered.Add(currentForm);
                                }
                                else
                                {
                                    considered = new();
                                    considered.Add(currentForm);
                                    LemmatizedForms.Add(currentForm.InitialWord,considered);
                                }
                                break;
                            }
                            case "dictionary":
                            {
                                Console.WriteLine("Parsing complete");
                                break;
                            }
                        }
                    }
                        break;
                }
            }
        }

        finally
        {
            if (reader != null)
                reader.Close();
        }
    }
}