import { InboxIcon } from './Icons.jsx'

function EmptyState({ icon, title, text, action, small }) {
    return (
        <div className={small ? 'empty empty-sm' : 'empty'}>

            <div className="empty-icon">
                {icon || <InboxIcon />}
            </div>

            <p className="empty-title">{title}</p>

            {text && <p className="empty-text">{text}</p>}

            {action && <div className="mt-5">{action}</div>}

        </div>
    )
}

export default EmptyState
