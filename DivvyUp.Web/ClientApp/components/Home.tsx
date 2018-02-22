import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import { Workers } from './Workers';
import { Failed } from './Failed';
import { Queues } from './Queues';

export class Home extends React.Component<RouteComponentProps<{}>, {}> {
    public render() {
        return <div>
            <Queues />
            <Workers />
            <Failed />
        </div>;
    }
}
