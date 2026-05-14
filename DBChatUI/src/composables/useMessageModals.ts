import { ref } from "vue";
import { getMessageSql, getMessageData } from "@/services/api";

export function useMessageModals() {
  const modalVisible = ref(false);
  const modalTitle = ref("");
  const modalContent = ref("");
  const modalType = ref(""); // "sql" | "data"

  async function showSql(msg: any) {
    modalType.value = "sql";
    try {
      modalTitle.value = "SQL";
      modalContent.value = "Loading...";
      modalVisible.value = true;

      const json = await getMessageSql(msg.id);
      modalContent.value = json.sql || "";
    } catch (err) {
      console.error("Error loading SQL modal:", err);
      modalContent.value = "Unable to load SQL.";
    }
  }

  async function showData(msg: any) {
    modalType.value = "data";
    try {
      modalTitle.value = "Data";
      modalContent.value = "Loading...";
      modalVisible.value = true;

      const json = await getMessageData(msg.id);
      modalContent.value = JSON.stringify(json, null, 2);
    } catch (err) {
      console.error("Error loading Data modal:", err);
      modalContent.value = "Unable to load Data.";
    }
  }

  async function copyContent() {
    try {
      await navigator.clipboard.writeText(modalContent.value);
    } catch (err) {
      console.error("Failed to copy content:", err);
    }
  }

  function downloadContent() {
    let ext = "txt";
    if (modalType.value === "sql") ext = "sql";
    if (modalType.value === "data") ext = "json";

    const blob = new Blob([modalContent.value], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${modalType.value}.${ext}`;
    a.click();
    URL.revokeObjectURL(url);
  }

  function downloadExcel() {
    try {
      const rows = JSON.parse(modalContent.value);
      if (!rows || !rows.length) return;

      const headers = Object.keys(rows[0]);
      const csv = [
        headers.join(","),
        ...rows.map((row: any) =>
          headers.map((h) => `"${String(row[h] ?? "").replace(/"/g, '""')}"`).join(",")
        ),
      ].join("\n");

      const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "data.csv";
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error("Unable to export Excel:", err);
      alert("Unable to export Excel.");
    }
  }

  return {
    modalVisible,
    modalTitle,
    modalContent,
    modalType,
    showSql,
    showData,
    copyContent,
    downloadContent,
    downloadExcel,
  };
}
