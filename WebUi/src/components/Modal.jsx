import { useEffect } from 'react'
import { XIcon } from './Icons.jsx'

/*
 * Dialog used for event details, volunteer lists and confirmations.
 * Closes on Escape or on a click outside the panel, and holds the page
 * still behind itself while it is open.
 */
function Modal({ title, subtitle, onClose, children, footer }) {

    useEffect(() => {

        function handleKey(event) {
            if (event.key === 'Escape') onClose()
        }

        document.addEventListener('keydown', handleKey)

        const previousOverflow = document.body.style.overflow
        document.body.style.overflow = 'hidden'

        return () => {
            document.removeEventListener('keydown', handleKey)
            document.body.style.overflow = previousOverflow
        }
    }, [onClose])

    return (
        <div className="modal-backdrop" onMouseDown={onClose}>
            <div
                className="modal"
                role="dialog"
                aria-modal="true"
                aria-label={title}
                onMouseDown={(event) => event.stopPropagation()}
            >

                <header className="modal-head">
                    <div>
                        <h2 className="modal-title">{title}</h2>
                        {subtitle && <p className="modal-sub">{subtitle}</p>}
                    </div>

                    <button
                        type="button"
                        className="icon-button"
                        onClick={onClose}
                        aria-label="Close"
                    >
                        <XIcon />
                    </button>
                </header>

                <div className="modal-body">{children}</div>

                {footer && <footer className="modal-foot">{footer}</footer>}

            </div>
        </div>
    )
}

export default Modal
