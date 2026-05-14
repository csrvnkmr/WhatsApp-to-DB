import { ref } from "vue";
import { useChatStore } from "@/stores/chat";
import { sendEmailResult } from "@/services/api";

export function useEmailResult() {
  const chat = useChatStore();

  const emailModalVisible = ref(false);
  const emailFrom = ref("");
  const emailTo = ref("");
  const emailCc = ref("");
  const emailSubject = ref("");
  const emailBody = ref("");
  const emailMessageId = ref(0);
  const emailSending = ref(false);

  function getQuestion(msg: any): string {
    const index = chat.messages.findIndex((x: any) => x.id === msg.id);

    if (index > 0) {
      for (let i = index - 1; i >= 0; i--) {
        if (chat.messages[i].role.toLowerCase() === "user") {
          return chat.messages[i].messageText;
        }
      }
    }
    return "InsightChat Result";
  }

  function emailResult(msg: any) {
    emailMessageId.value = msg.id;

    // default from
    emailFrom.value = localStorage.getItem("username") || "";
    emailTo.value = "";
    emailCc.value = "";

    // Find previous user question in chat
    const question = getQuestion(msg);
    emailSubject.value = question;

    // default body = current answer
    emailBody.value = msg.messageText || "";
    emailModalVisible.value = true;
  }

  async function sendEmail() {
    if (!emailFrom.value || !emailTo.value) {
      alert("Please enter From and To.");
      return;
    }

    if (emailSending.value) return;

    try {
      emailSending.value = true;

      await sendEmailResult({
        messageId: emailMessageId.value,
        from: emailFrom.value,
        to: emailTo.value,
        cc: emailCc.value,
        subject: emailSubject.value,
        body: emailBody.value,
      });

      alert("Email sent successfully.");
      emailModalVisible.value = false;
    } catch (err: any) {
      alert(err?.message || "Unable to send email.");
    } finally {
      emailSending.value = false;
    }
  }

  return {
    emailModalVisible,
    emailFrom,
    emailTo,
    emailCc,
    emailSubject,
    emailBody,
    emailSending,
    emailResult,
    sendEmail,
    getQuestion
  };
}
