using System.Collections.Generic;

namespace WhatsAppToDB.Eval
{
    public class EvalModel
    {
        public string Provider { get; set; } = "";
        public string Model { get; set; } = "";
    }
/*
"Database":<database>,
"Questions":[q1, q2...],
"Models:[
 { "Provider":provider,
 "Model":model
 }, {..}
]
*/
    public class EvalRunRequest
    {
        public string Database { get; set; } = "";

        public List<string> Questions { get; set; } = new List<string>();

        public List<EvalModel> Models { get; set; } = new List<EvalModel>();
    }
}
