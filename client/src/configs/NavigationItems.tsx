import React, { JSX } from 'react';
import { FaCommentDots, FaUserAlt, FaHandshake, FaUserPlus, FaCog, FaBalanceScale } from 'react-icons/fa';
import ChatRoom from '../components/ChatRoom/ChatRoom';
import Settings from '../components/Settings/Settings';
import Compliance from '../components/Compliance/Compliance';

export interface NavItem {
  path: string;
  label: string;
  icon: JSX.Element;
  emoji?: string; // Optional property for emoji
  element: React.ReactNode;
}

export const navItems: NavItem[] = [
  
  {
    path: '/Settings',
    label: 'Settings',
    icon: <FaCog className="nav-icon" />,
    emoji: '⚙️',
    element: <Settings />,
  },
  {
    path: '/Compliance',
    label: 'Compliance',
    icon: <FaBalanceScale className="nav-icon" />,
    emoji: '⚖️',
    element: <Compliance />,
  },
];
