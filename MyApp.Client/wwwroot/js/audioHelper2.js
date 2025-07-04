window.audioHelper2 = {
    announceWard: function (user, text) {
        const message = `ขอเชิญคุณ${user}มา${text}`; // Example: "ขอเชิญคุณสมชายมาซักประวัติ"
        const utterance = new SpeechSynthesisUtterance(message);
        utterance.lang = 'th-TH';
        speechSynthesis.speak(utterance);
    }
};


