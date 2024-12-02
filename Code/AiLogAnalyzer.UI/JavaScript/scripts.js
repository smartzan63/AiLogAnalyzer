function highlightCodeBlocks() {
    try {
        document.querySelectorAll('code[class*="language-"]').forEach((block) => {
            if (!block.classList.contains('highlighted')) {
                hljs.highlightElement(block);
                block.classList.add('highlighted');
            }
        });
    } catch (error) {
        console.error('Error highlighting code blocks:', error);
    }
}

function showAgentMessageWithHighlights(message) {
    let html = marked.parse(message);
    let lastDiv = document.body.lastElementChild;
    
    if (lastDiv && lastDiv.classList.contains('agentMessage')) {
        //console.log('Updating last agentMessage div, lastDiv:', lastDiv);
        lastDiv.innerHTML = html;
    } else {
        console.log('Creating new agentMessage div');
        let newDiv = document.createElement('div');
        newDiv.classList.add('agentMessage');
        newDiv.innerHTML = html;
        document.body.appendChild(newDiv);
    }

    highlightCodeBlocks();
}

function showUserMessage(message) {
    console.log('User message:', message);
    
    // Probably later we will use marked, but not now
    //let html = marked.parse(DOMPurify.sanitize(message));
    let sanitizedMessage = DOMPurify.sanitize(message);

    let newUserDiv = document.createElement('div');
    newUserDiv.classList.add('userMessage');
    newUserDiv.innerHTML = sanitizedMessage;

    document.body.appendChild(newUserDiv);

    // In some edge cases it doesn't work as it should, so plain text for now
    //highlightCodeBlocks();
    
    window.scrollTo(0, document.body.scrollHeight);
}

document.addEventListener('DOMContentLoaded', function () {

    window.addEventListener('resize', function () {
        {
            document.body.style.zoom = Math.min(window.innerWidth / document.body.scrollWidth, 1);
        }
    });
    document.body.style.zoom = Math.min(window.innerWidth / document.body.scrollWidth, 1);
});