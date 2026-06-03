public class EvalTableSqls
{
    public const string CreateEvalRunTable = @"
    CREATE TABLE IF NOT EXISTS eval_run (
        eval_run_id       INTEGER PRIMARY KEY AUTOINCREMENT,
        username          TEXT    NOT NULL,
        usertoken         TEXT    NOT NULL,
        database          TEXT    NOT NULL,
        status            TEXT    NOT NULL DEFAULT 'Running',   -- Running / Completed / Aborted
        started_at        DATETIME NOT NULL DEFAULT (datetime('now')),
        completed_at      DATETIME,
        total_cases       INTEGER NOT NULL DEFAULT 0,
        passed_count      INTEGER NOT NULL DEFAULT 0,
        failed_count      INTEGER NOT NULL DEFAULT 0,
        error_count       INTEGER NOT NULL DEFAULT 0
    );";

    public const string CreateEvalCaseTable = @"
    CREATE TABLE IF NOT EXISTS eval_case (
        eval_case_id              INTEGER PRIMARY KEY AUTOINCREMENT,
        eval_run_id               INTEGER  NOT NULL REFERENCES eval_run(eval_run_id),
        module                    TEXT,
        sequence                  INTEGER  NOT NULL DEFAULT 0,
        input_text                TEXT     NOT NULL,
        module_name          TEXT     NOT NULL,
        ground_truth_sql          TEXT     NOT NULL,
        ground_truth_result_json  TEXT,
        ground_truth_result_file  TEXT,
        started_at        DATETIME,
        completed_at      DATETIME

    );";

    public const string CreateEvalCaseInferenceTable = @"
    CREATE TABLE IF NOT EXISTS eval_case_inference (
        eval_inference_id      INTEGER PRIMARY KEY AUTOINCREMENT,
        eval_case_id           INTEGER  NOT NULL REFERENCES eval_case(eval_case_id),
        provider_name          TEXT     NOT NULL,
        model_name             TEXT     NOT NULL,
        generated_sql          TEXT,
        inference_result_json  TEXT,
        inference_result_file  TEXT,
        match                  INTEGER,                          -- 0/1 raw judge result before override
        answers_question       INTEGER,                          -- 0/1
        judge_confidence       TEXT,                             -- high / medium / low
        judge_reasoning        TEXT,
        differences            TEXT,
        was_overridden         INTEGER  NOT NULL DEFAULT 0,      -- 0/1
        verdict                TEXT,                             -- Pass / Fail / Error
        prompt_tokens          INTEGER,
        completion_tokens      INTEGER,
        latency_ms             INTEGER,
        started_at             DATETIME,
        completed_at           DATETIME
    );";

    public const string CreateEvalIndices = @"
    -- Indexes for the most common query patterns
    CREATE INDEX IF NOT EXISTS idx_eval_case_run      ON eval_case(eval_run_id);
    CREATE INDEX IF NOT EXISTS idx_eval_case_module   ON eval_case(module);
    CREATE INDEX IF NOT EXISTS idx_inference_case     ON eval_case_inference(eval_case_id);
    CREATE INDEX IF NOT EXISTS idx_inference_verdict  ON eval_case_inference(verdict);
    ";

    
}