function ThenRegistration(registration) {
    setInterval(() => {
        registration.update();
    }, 60 * 1000); // 60000ms -> check each minute


    if (registration.waiting) {
        invokeServiceWorkerUpdateFlow(registration)
    }

    registration.addEventListener('updatefound', () => {
        try {
            if (registration.installing) {
                registration.installing.addEventListener('statechange', () => {
                    if (registration.waiting) {
                        if (navigator.serviceWorker.controller) {
                            invokeServiceWorkerUpdateFlow(registration);
                        }
                    }
                });

            }
        }
        catch (e) {
            console.log(e);
        }
    });
}

function invokeServiceWorkerUpdateFlow(registration) {
    if (registration.waiting) {
        var updateBtn = document.querySelector("#updateBtn");
        if (updateBtn.style.display != "block") {
            updateBtn.style.display = "block";
            updateBtn.addEventListener('click', () => {
                updateBtn.querySelector(".spinner-border")?.classList.remove("d-none");
                var p = registration.waiting || registration.active || registration.installing;
                p.postMessage('SKIP_WAITING');
            })
        }
    }
}