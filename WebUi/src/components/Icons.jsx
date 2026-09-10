/*
 * Small stroke icon set. Everything draws in currentColor and sizes from the
 * class it is given, so an icon inherits the colour of the text next to it.
 */

function Icon({ children, className = "h-5 w-5", ...rest }) {
    return (
        <svg
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.75"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
            className={className}
            {...rest}
        >
            {children}
        </svg>
    )
}

export const CalendarIcon = (props) => (
    <Icon {...props}>
        <rect x="3" y="5" width="18" height="16" rx="2" />
        <path d="M8 3v4M16 3v4M3 10h18" />
    </Icon>
)

export const ClockIcon = (props) => (
    <Icon {...props}>
        <circle cx="12" cy="12" r="9" />
        <path d="M12 7v5l3 2" />
    </Icon>
)

export const MapPinIcon = (props) => (
    <Icon {...props}>
        <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 1 1 16 0Z" />
        <circle cx="12" cy="10" r="3" />
    </Icon>
)

export const SearchIcon = (props) => (
    <Icon {...props}>
        <circle cx="11" cy="11" r="7" />
        <path d="m20 20-3.6-3.6" />
    </Icon>
)

export const XIcon = (props) => (
    <Icon {...props}>
        <path d="M18 6 6 18M6 6l12 12" />
    </Icon>
)

export const PlusIcon = (props) => (
    <Icon {...props}>
        <path d="M12 5v14M5 12h14" />
    </Icon>
)

export const UsersIcon = (props) => (
    <Icon {...props}>
        <path d="M16 19v-1a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v1" />
        <circle cx="9.5" cy="7" r="3.5" />
        <path d="M21 19v-1a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
    </Icon>
)

export const PencilIcon = (props) => (
    <Icon {...props}>
        <path d="M4 20h4L18.5 9.5a2.83 2.83 0 0 0-4-4L4 16v4Z" />
        <path d="m13.5 6.5 4 4" />
    </Icon>
)

export const TrashIcon = (props) => (
    <Icon {...props}>
        <path d="M4 7h16M10 11v6M14 11v6" />
        <path d="m5.5 7 1 12a2 2 0 0 0 2 1.9h7a2 2 0 0 0 2-1.9l1-12" />
        <path d="M9 7V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2" />
    </Icon>
)

export const MailIcon = (props) => (
    <Icon {...props}>
        <rect x="3" y="5" width="18" height="14" rx="2" />
        <path d="m3.6 7.2 8.4 5.9 8.4-5.9" />
    </Icon>
)

export const PhoneIcon = (props) => (
    <Icon {...props}>
        <path d="M21 16.9v2.6a2 2 0 0 1-2.2 2 19.6 19.6 0 0 1-8.5-3A19.3 19.3 0 0 1 4.4 12a19.6 19.6 0 0 1-3-8.6A2 2 0 0 1 3.4 1H6a2 2 0 0 1 2 1.7c.13.96.36 1.9.7 2.8a2 2 0 0 1-.46 2.1L7.1 8.8a16 16 0 0 0 6 6l1.2-1.2a2 2 0 0 1 2.1-.45c.9.34 1.84.57 2.8.7a2 2 0 0 1 1.8 2.05Z" />
    </Icon>
)

export const BuildingIcon = (props) => (
    <Icon {...props}>
        <path d="M4 21V6a2 2 0 0 1 2-2h7a2 2 0 0 1 2 2v15" />
        <path d="M15 10h3a2 2 0 0 1 2 2v9" />
        <path d="M8 8h3M8 12h3M8 16h3M2.5 21h19" />
    </Icon>
)

export const HeartIcon = (props) => (
    <Icon {...props}>
        <path d="M12 20.5S3.5 15.6 3.5 9.9A4.9 4.9 0 0 1 12 6.6a4.9 4.9 0 0 1 8.5 3.3c0 5.7-8.5 10.6-8.5 10.6Z" />
    </Icon>
)

export const CheckCircleIcon = (props) => (
    <Icon {...props}>
        <circle cx="12" cy="12" r="9" />
        <path d="m8.5 12.2 2.4 2.4 4.6-4.9" />
    </Icon>
)

export const AlertIcon = (props) => (
    <Icon {...props}>
        <circle cx="12" cy="12" r="9" />
        <path d="M12 7.5v5M12 16.2h.01" />
    </Icon>
)

export const ArrowRightIcon = (props) => (
    <Icon {...props}>
        <path d="M5 12h14M13 6l6 6-6 6" />
    </Icon>
)

export const LogOutIcon = (props) => (
    <Icon {...props}>
        <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
        <path d="m16 17 5-5-5-5M21 12H9" />
    </Icon>
)

export const InboxIcon = (props) => (
    <Icon {...props}>
        <path d="M5 5h14l2.5 8v5a2 2 0 0 1-2 2h-15a2 2 0 0 1-2-2v-5L5 5Z" />
        <path d="M2.5 13H8l1.5 2.5h5L16 13h5.5" />
    </Icon>
)

export const SparkIcon = (props) => (
    <Icon {...props}>
        <path d="M12 3.5 13.8 9l5.7 1.8-5.7 1.8L12 18.5l-1.8-5.9L4.5 10.8 10.2 9 12 3.5Z" />
    </Icon>
)
